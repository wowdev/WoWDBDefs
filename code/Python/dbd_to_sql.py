#!/usr/bin/env python3

import dbd
import os
from argparse import ArgumentParser
from collections import defaultdict
from glob import glob

script_dir:str = os.path.dirname(os.path.abspath(__file__));

parser = ArgumentParser();
group = parser.add_mutually_exclusive_group();
group.add_argument('--layout', type=str, help="target layout, e.g. '90747013'");
group.add_argument('--build', type=str, help="target build, e.g. '10.0.0.43342'");
parser.add_argument('dbds', type=str, nargs='*', help='directory with / list of for dbd files to process');
parser.add_argument('--output', type=str, default=os.path.join(script_dir, 'dbds.sql'), help='file or directory to dump sql to');
args = parser.parse_args();

dbds:list[str] = args.dbds or os.path.join(
	os.path.dirname( # WoWDBDefs/
	os.path.dirname( # code/
		script_dir   # Python/
	)),
	'definitions'
);
if not dbds[0].endswith(dbd.file_suffix):
	dbds = glob(os.path.join(dbds[0], '*.dbd'));

print(f"Found {len(dbds)} definitions to process");

outfile:str = args.output;
outdir:str = '';
if outfile.endswith('.sql'):
	with open(outfile, 'w') as file:
		file.write("SET SESSION FOREIGN_KEY_CHECKS=0;\n");
else:
	if not os.path.isdir(outfile):
		os.makedirs(outfile);
	outdir = outfile;
	outfile = None;

print(f"Outputting to {outdir or outfile}");

def get_sql_type(type:str, int_width:int=0, is_unsigned:bool=False)->str:
	type = {
		'uint'      : 'int',
		#'int'       : 'int',
		'locstring' : 'text',
		'string'    : 'text',
		#'float'     : 'float'
	}.get(type, type);

	default = {
		'int'   : '0',
		'text'  : "''",
		'float' : '0.0'
	}.get(type, 'NULL');

	type = {
		8  : 'tinyint',
		16 : 'smallint',
		32 : 'mediumint',
		64 : 'bigint'
	}.get(int_width, type);

	if is_unsigned:
		type += ' unsigned';

	return f"{type} DEFAULT {default}";

keys:dict[str, dict[str, str]] = defaultdict(dict);
def process_dbd(file:str)->bool:
	parsed:dbd.dbd_file = dbd.parse_dbd_file(file);
	if not len(parsed.definitions):
		print(f"No definitions found in {file}! Skipping");
		return False;

	dirname:str = os.path.dirname(file);
	name:str = os.path.splitext(os.path.basename(file))[0];
	if keys.get(name, None):
		return True; # Already processed

	types:dict[str,str] = {};
	foreigns:dict[str,list[str]] = {};
	column:dbd.column_definition
	for column in parsed.columns:
		types[column.name] = column.type;
		if column.foreign:
			foreigns[column.name] = column.foreign;

	definition:dbd.definitions = None;
	if args.layout:
		definition = next(defn for defn in parsed.definitions if args.layout in defn.layouts);
		if not definition:
			print(f"No definition found for layout {args.layout}! Skipping");
			return False;
	elif args.build:
		definition = next(defn for defn in parsed.definitions if args.build in defn.builds);

	if not definition:
		definition = max(parsed.definitions, key =
			lambda defn: max(getattr(build, 'build', getattr(build[-1], 'build', 0)) for build in defn.builds)
		);

	# TODO: include comments in sql
	columns:list[str] = [];
	indices:list[str] = [];
	fkeys:list[str]   = [];
	entry:dbd.definition_entry
	for entry in definition.entries:
		sql_type:str = get_sql_type(types.get(entry.column), entry.int_width, entry.is_unsigned);
		suffix:str = '';
		if 'id' in entry.annotation:
			suffix = 'PRIMARY KEY';
			keys[name][entry.column] = sql_type;
		elif (foreign := foreigns.get(entry.column, None)):
			# TODO: unhack!
			if not keys.get(foreign.table.string, {}).get(foreign.column.string, None):
				foreign_dbd:str = next((f for f in dbds if os.path.basename(f) == f"{foreign.table}.dbd"), None);
				if foreign_dbd:
					if not process_dbd(foreign_dbd):
						print(f"Could not process table {foreign.table} referenced by {name}.{entry.column}");
						return False;
				if not foreign_dbd:
					print(f"FK {name}.{entry.column} references {foreign.column} in {foreign.table} which was not supplied");

			sql_type = keys[foreign.table.string].get(foreign.column.string, None) or sql_type;
			fkeys.append(
				f"FOREIGN KEY (`{entry.column}`) "
				f"REFERENCES `{foreign.table}` (`{foreign.column}`) "
				'ON DELETE NO ACTION ON UPDATE NO ACTION'
			);
		elif 'relation' in entry.annotation:
			keys[name][entry.column] = sql_type;
			indices.append(f"INDEX (`{entry.column}`)");
			# TODO: Get self-referencing keys to work
			#fkeys.append(f"FOREIGN KEY (`{entry.column}`) REFERENCES `{name}` (`{entry.column}`) ON DELETE NO ACTION ON UPDATE NO ACTION");

		columns.append(f"`{entry.column}` {sql_type} {suffix}");

	fields:list[str] = [','.join(columns)];
	if len(indices):
		fields.append(', '.join(indices));
	if len(fkeys):
		fields.append(', '.join(fkeys));

	stmt:str = f"CREATE OR REPLACE TABLE `{name}` ({', '.join(fields)})";

	if outfile:
		with open(outfile, 'a') as file:
			file.write(f"{stmt};\n");
	elif outdir:
		with open(os.path.join(outdir, f"{name}.sql"), 'w') as file:
			file.write(stmt);

	return True;

for file in dbds:
	process_dbd(file);

if outfile:
	with open(outfile, 'a') as file:
		file.write("SET SESSION FOREIGN_KEY_CHECKS=1;\n");

print('Done.');
