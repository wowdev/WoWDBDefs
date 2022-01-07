#! /usr/bin/env python3
# This is a kind of janky script to generte a mostly reasonable MySQL schema
# for given DBDs and build, including appropriate indexes and foreign keys.
# The way the bundled dbd library structures things makes this a bit more
# irritating than it could otherwise be.
#
# General usage: ./dbd_to_mysql.py | mysql -u username
#
# I'm not particularly proud of most of this code.  --A

import argparse
import sys
from typing import Dict, List, Set

import dbd

from ppretty import ppretty


def errout(msg: str) -> None:
    print(msg, file=sys.stderr)


# Look through all our tables and find columns that are used as a reference
# for a foreign key, so that we can add an index on them.
def get_fk_cols(dbds: Dict[str, dbd.dbd_file]) -> Dict[str, bool]:
    fks: Dict[str, bool] = {}

    for _, data in dbds.items():
        for coldata in data.columns:
            if coldata.foreign:
                # FIXME: Maybe use a tuple as a key instead
                key = f"{coldata.foreign.table}.{coldata.foreign.column}"
                fks[f"{key}"] = True

    return fks


# For a given table, find the columns that are actually present in a speciifc
# build (specified as a build number)
def get_build_cols(dbd_data: dbd.dbd_file, use_build: int) -> List[str]:
    for definition in dbd_data.definitions:
        for build in definition.builds:
            if getattr(build, "build", None) == use_build:
                return [e.column for e in definition.entries]

    return []


# Generate a list of the columns actually present in a specific build for
# every table in our dataset.
def getbuildcols(dbds: Dict[str, dbd.dbd_file], use_build: int) -> Dict[str, Set[str]]:
    buildcols: Dict[str, Set[str]] = {}

    for table, data in dbds.items():
        bc = set(get_build_cols(data, use_build))

        if len(bc) > 0:
            buildcols[table] = bc

    return buildcols


# take a given table's parsed data, a list of foreign keys, and a list of
# present columns, and generate a bunch of MySQL `CREATE TABLE` statements.
# Returns a list of statements to be executed in an `ALTER TABLE` after all
# of the tables are created, since you can't do things like create foreign
# keys until the tables they reference exist.
def dumpdbd(dbname: str, table: str, dbd_data: dbd.dbd_file,
            fkcols: Dict[str, bool], bcols: Dict[str, Set[str]]) -> List[str]:
    create_lines: List[str] = []  # lines for things we need to create
    create_idxs: List[str] = []  # lines for indexes
    deferred: List[str] = []  # lines to execute in an `ALTER` at the very end

    # a table that has no columns in the current build doesn't exist in the
    # current build
    if table not in bcols:
        return []

    table_cols = {column.name for column in dbd_data.columns}
    remaining_cols = table_cols.intersection(bcols[table])

    # If we have no columns left after we filter out what's not present
    # in this build, warn and do nothing.
    if len(remaining_cols) == 0:
        errout(f"WARNING: {table} exists in this build, but needed columns don't exist?!")
        return []

    # Not sure everything is guaranteed to have a PK, so add one of our
    # own if there's not an ID column
    if "ID" not in remaining_cols:
        create_lines.append("  _id INT UNSIGNED NOT NULL")

    # Iterate through all the parsed columns (the parser will generate a
    # complete set, regardless of whether they exist in a given build)
    for column in dbd_data.columns:
        # skip columns not in this build
        if column.name not in remaining_cols:
            continue

        # Create an index for FK-referenced columns (except for the PK, which
        # gets handled separately). We're going to keep these separately and
        # add them at the end of the `CREATE`
        if column.name != "ID" and f"{table}.{column.name}" in fkcols:
            create_idxs.append(column.name)

        if column.type == "int":
            create_lines.append(f"  `{column.name}` INT UNSIGNED")
        elif column.type == "float":
            create_lines.append(f"  `{column.name}` FLOAT")
        elif column.type in ["string", "locstring"]:
            create_lines.append(f"  `{column.name}` MEDIUMTEXT")
        else:
            errout(f"WARNING: Unknown type {column.type} for {table}.{column.name}")

    # save all the indexes for the end of the `CREATE`
    if "ID" in remaining_cols:
        create_lines.append("  PRIMARY KEY(ID)")
    else:
        create_lines.append("  PRIMARY KEY (_id)")

    for idx in create_idxs:
        create_lines.append(f"  INDEX `{idx}_idx` (`{idx}`)")

    # Generate statements for appropriate foreign keys, which will be returned
    # from this function to be added at the end after all the tables have been
    # created.
    for column in dbd_data.columns:
        if column.name not in remaining_cols:
            continue

        if column.foreign:
            t = str(column.foreign.table)
            c = str(column.foreign.column)

            # Don't complain about the FileData table not existing, since
            # everything just uses FDIDs directly now and this FK is still
            # everywhere because FK info isn't versioned in the dbd defs
            if t == "FileData":
                continue

            # This was an across the board change, just Make It Work™
            if t == "SoundEntries":
                t = "SoundKit"

            # Make sure the destination table of the FK exists in this build
            if t not in bcols:
                errout(
                    f"WARNING: Foreign key for {table}.{column.name} references non-existent table {t}")
                continue

            # Make sure the dsestination column of the FK exists in this build
            if c not in bcols[t]:
                errout(
                    f"WARNING: Foreign key {table}.{column.name} references non-existent column {t}.{c}")
                continue

            deferred.append(
                f"  ADD CONSTRAINT `{table}_{column.name}` FOREIGN KEY (`{column.name}`) REFERENCES `{dbname}`.`{t}` (`{c}`)")

    # Generate the actual `CREATE` statement
    print(f"\nCREATE TABLE IF NOT EXISTS `{dbname}`.`{table}` (")
    print(",\n".join(create_lines))
    print(");")

    return deferred


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--definitions", dest="definitions", type=str, action='store',
        default="../../definitions", help="location of .dbd files")
    parser.add_argument(
        "--build", dest="build", type=int, default=41488,
        help="build number (and only build number) to use for parsing")
    parser.add_argument(
        "--dbname", dest="dbname", type=str, default="wowdbd",
        help="name of MySQL database to generate create statements for")

    # --only is disabled for now, since using it will cause FKs to be wrong
    # if it's used to try to generate an updated schema w/o parsing everything
    # parser.add_argument(
    #     "--only", dest="only", type=str, action='append',
    #     help="parse only these tables")

    args = parser.parse_args()

    # dbds = {}
    # if args.only:
    #   for table in args.only:
    #     dbds[table] = dbd.parse_dbd_file(os.path.join(args.definitions, "{}{}".format(table, dbd.file_suffix)))
    # else:
    #   dbds = dbd.parse_dbd_directory(args.definitions)

    dbds = dbd.parse_dbd_directory(args.definitions)

    fkcols = get_fk_cols(dbds)  # foreign key columns
    bcols = getbuildcols(dbds, args.build)  # columns for specific build

    deferred = {}  # deferred statements to add to `ALTER` at the end

    # No in-place updates -- just drop and recreate the entire database
    print(f"DROP DATABASE IF EXISTS {args.dbname};")
    print(f"CREATE DATABASE {args.dbname};")

    for table, data in dbds.items():
        deferred[table] = dumpdbd(args.dbname, table, data, fkcols, bcols)

    for table, lines in deferred.items():
        if len(lines) > 0:
            print(f"\nALTER TABLE `{args.dbname}`.`{table}`")
            print(",\n".join(lines))
            print(";")

    return 0


if __name__ == "__main__":
    sys.exit(main())
