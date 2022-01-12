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
import os
import pickle
import re
import sys
from typing import Dict, List, Optional, Set

import dbdwrapper
# from ppretty import ppretty


def errout(msg: str) -> None:
    print(msg, file=sys.stderr)


def get_fk_cols(dbds: dbdwrapper.DbdDirectory) -> Set[str]:
    """
    Look through all our tables and find columns that are used as a reference
    for a foreign key, so that we can add an index on them later.

    :param dbds: The data structure from a dbd directory parsed by dbdwrapper
    :type dbds: dbdwrapper.DbdDirectory
    :return: A set containing the names of all FKs, in the format of `table.column`
    :rtype: Set[str]
    """
    fks: Set[str] = set()

    for _, data in dbds.items():
        for _, coldata in data.columns.items():
            if coldata.fk:
                # FIXME: Maybe use a tuple as a key instead
                key = f"{coldata.fk.table}.{coldata.fk.column}"
                fks.add(key)

    return fks


int_sizemap = {
    8: "TINYINT",
    16: "SMALLINT",
    32: "INT",
    64: "BIGINT",
}

def coltype_string(column: dbdwrapper.DbdVersionedCol) -> str:
    """
    Generate the type string for a given column, based on DBD data

    :param column: A versioned column struct from DBD data
    :type column: dbdwrapper.DbdVersionedCol
    :return: A string that can be used in a column of a `CREATE TABLE` statement
    :rtype: str
    """
    annotations = ""
    if len(column.annotation) > 0:
        annotations = "(annotations: " + ", ".join(column.annotation) + ")"

    # Theoretically column def or entry def could have a comment,
    # but most/all seem to be on the global column def, so ... just use that.
    comments = ""
    if column.definition.comment is not None:
        comments = column.definition.comment.replace("'", "\\'")

    sql_comment_string = ""
    if annotations or comments:
        if annotations and comments:  # feels sloppy
            sql_comment_string = f" COMMENT '{comments} {annotations}'"
        else:
            sql_comment_string = f" COMMENT '{comments}{annotations}'"

    if column.definition.type == "int":
        assert column.int_width is not None
        int_string = int_sizemap.get(column.int_width, "INT")
        if column.is_unsigned:
            return f"  `{column.name}` {int_string} UNSIGNED{sql_comment_string}"
        else:
            return f"  `{column.name}` {int_string}{sql_comment_string}"

    if column.definition.type == "float":
        return f"  `{column.name}` FLOAT{sql_comment_string}"

    if column.definition.type in ["string", "locstring"]:
        return f"  `{column.name}` MEDIUMTEXT{sql_comment_string}"

    raise ValueError(f"Unknown column type: {column.definition.type}")


def dumpdbd(dbname: str, table: str, all_data: dbdwrapper.DbdVersionedView,
            table_data: dbdwrapper.DbdVersionedCols, fkcols: Set[str]) -> List[str]:
    """
    Take the parsed data, the build-specific view, and a list of foreign keys,
    and generate a bunch of MySQL `CREATE TABLE` statements. Returns a list of
    statements to be executed in an `ALTER TABLE` after all of the tables are
    created, since you can't do things like create foreign keys until
    the tables they reference exist.

    :param dbname: [description]
    :type dbname: str
    :param table: [description]
    :type table: str
    :param all_data: [description]
    :type all_data: dbdwrapper.DbdVersionedView
    :param table_data: [description]
    :type table_data: dbdwrapper.DbdVersionedCols
    :param fkcols: [description]
    :type fkcols: Set[str]
    :return: [description]
    :rtype: List[str]
    """

    create_lines: List[str] = []  # lines for things we need to create
    create_idxs: List[str] = []  # lines for indexes
    deferred: List[str] = []  # lines to execute in an `ALTER` at the very end

    # So that we can find our PK as we iterate through the table
    id_col = None

    # cycle through every column in our view and generate SQL
    for _, column in table_data.items():
        # id column?
        if "id" in column.annotation:
            id_col = column.name

        # If this column is referenced by another table/column's foreign key,
        # generate an index for it (unless this column is already the PK).
        # Indexes get kept until the end so that we can stuff them at the
        # bottom of the `CREATE` block
        if column.name != id_col and f"{table}.{column.name}" in fkcols:
            create_idxs.append(f"  INDEX `{column.name}_idx` (`{column.name}`)")

        if column.definition.type in ["string", "locstring"]:
            create_idxs.append(f"  FULLTEXT `{column.name}_idx` (`{column.name}`)")

        create_lines.append(coltype_string(column))

    # Occasional things might not have a PK annotated, so make sure we still
    # have a PK if not
    if id_col is None:
        create_lines.insert(0, "  _id INT UNSIGNED NOT NULL")
        create_lines.append("  PRIMARY KEY (_id)")
    else:
        create_lines.append(f"  PRIMARY KEY({id_col})")

    # Add in any index creation we had stored for now
    create_lines.extend(create_idxs)

    # Generate statements for appropriate foreign keys, which will be returned
    # from this function to be added at the end after all the tables have been
    # created.
    for _, column in table_data.items():
        if column.definition.fk is not None:
            t = str(column.definition.fk.table)
            c = str(column.definition.fk.column)

            # Don't complain about the FileData table not existing, since
            # everything just uses FDIDs directly now, but the FK annotation
            # still exists because it's a part of the defs structure that isn't
            # versioned
            if t == "FileData":
                continue

            # This was an across the board change, just Make It Work™
            if t == "SoundEntries":
                t = "SoundKit"

            # Make sure the destination table of the FK exists in this build
            if t not in all_data:
                errout(
                    f"WARNING: Foreign key for {table}.{column.name} references non-existent table {t}")
                continue

            # Make sure the dsestination column of the FK exists in this build
            if c not in all_data[t]:
                errout(
                    f"WARNING: Foreign key {table}.{column.name} references non-existent column {t}.{c}")
                continue

            deferred.append(
                f"  ADD CONSTRAINT `{table}_{column.name}` FOREIGN KEY (`{column.name}`) REFERENCES `{dbname}`.`{t}` (`{c}`)")

    # Generate the actual `CREATE` statement
    # FIXME: include comment w/ layout hash(s), git source info, and file comments
    print(f"\nCREATE TABLE IF NOT EXISTS `{dbname}`.`{table}` (")
    print(",\n".join(create_lines))
    print(");")

    return deferred


def build_string_regex(arg_value, pat=re.compile(r"^\d+\.\d+\.\d+\.\d+$")) -> str:
    if not pat.match(arg_value):
        raise argparse.ArgumentTypeError("invalid build string (try e.g. '9.1.5.41488')")

    return arg_value


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--definitions", dest="definitions", type=str, action='store',
        default="../../definitions", help="location of .dbd files")
    parser.add_argument(
        "--build", dest="build", type=build_string_regex, default="9.1.5.41488",
        help="full build number to use for parsing")
    parser.add_argument(
        "--dbname", dest="dbname", type=str, default="wowdbd",
        help="name of MySQL database to generate create statements for")
    parser.add_argument(
        "--no-pickle", dest="no_pickle", action='store_true', default=False,
        help="don't use or create pickled data file")

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

    dbds = None
    if not args.no_pickle and os.path.exists(os.path.join(args.definitions, "dbd_to_mysql.pickle")):
        # FIXME: Can we make pickling (or other caching) automatic in dbdwrapper?
        errout("NOTICE: Reading pickled dbd data from disk")
        with open(os.path.join(args.definitions, "dbd_to_mysql.pickle"), "rb") as f:
            try:
                dbds = pickle.load(f)
            except Exception as e:
                errout("WARNING: failed to read pickled data from disk")

    if dbds is None:
        errout("NOTICE: Directly parsing dbd data and pickling to disk")
        dbds = dbdwrapper.parse_dbd_directory(args.definitions)
        if not args.no_pickle:
            with open(os.path.join(args.definitions, "dbd_to_mysql.pickle"), "wb") as f:
                pickle.dump(dbds, f)

    build = dbdwrapper.BuildId.from_string(args.build)
    view = dbds.get_view(build)
    fkcols = get_fk_cols(dbds)  # foreign key columns

    # deferred statements to add to `ALTER` at the end
    deferred = {}

    # No in-place updates -- just drop and recreate the entire database
    print(f"DROP DATABASE IF EXISTS {args.dbname};")
    print(f"CREATE DATABASE {args.dbname};")

    for table, data in view.items():
        deferred[table] = dumpdbd(args.dbname, table, view, data, fkcols)

    for table, lines in deferred.items():
        if len(lines) > 0:
            print(f"\nALTER TABLE `{args.dbname}`.`{table}`")
            print(",\n".join(lines))
            print(";")

    return 0


if __name__ == "__main__":
    sys.exit(main())
