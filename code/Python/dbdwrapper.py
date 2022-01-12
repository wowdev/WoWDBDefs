#!/usr/bin/env python3
# simplify the results of parsing a dbd w/ the dbd parser library
import dataclasses
import os
import re
import sys
from dataclasses import dataclass
from typing import (Any, Dict, List, Literal, Optional, Set, Tuple, Type,
                    TypeVar, Union)

import dbd
from ppretty import ppretty

BuildIdOrTuple = Union['BuildId', Tuple[int, int, int, int]]

@dataclass(init=True, repr=True, eq=True, frozen=True)
class BuildId:
    major: int
    minor: int
    patch: int
    build: int

    @classmethod
    def from_dbd(cls, src: dbd.build_version) -> 'BuildId':
        return cls(
            major=src.major,
            minor=src.minor,
            patch=src.patch,
            build=src.build
        )

    @classmethod
    def from_tuple(cls, src: Tuple[int, int, int, int]) -> 'BuildId':
        return cls(*src)

    @classmethod
    def from_string(cls, src: str) -> 'BuildId':
        if not re.match(r'^\d+\.\d+\.\d+\.\d+$', src):
            raise ValueError(f"Invalid build id string: {src}")

        major, minor, patch, build = src.split('.')
        return cls(
            major=int(major),
            minor=int(minor),
            patch=int(patch),
            build=int(build)
        )

    @staticmethod
    def build_compare(buildid1: 'BuildId', buildid2: 'BuildId') -> int:
        if buildid1.major != buildid2.major:
            return buildid1.major - buildid2.major
        if buildid1.minor != buildid2.minor:
            return buildid1.minor - buildid2.minor
        if buildid1.patch != buildid2.patch:
            return buildid1.patch - buildid2.patch
        return buildid1.build - buildid2.build

    # def __eq__(self, other: object) -> bool:
    #     if not isinstance(other, BuildId) and not isinstance(other, tuple):
    #         return NotImplemented

    #     if isinstance(other, tuple):
    #         other = BuildId(*other)

    #     return BuildId.build_compare(self, other) == 0

    def __lt__(self, other: BuildIdOrTuple) -> bool:
        if not isinstance(other, BuildId):
            other = BuildId(*other)

        return BuildId.build_compare(self, other) < 0

    def __gt__(self, other: BuildIdOrTuple) -> bool:
        if not isinstance(other, BuildId):
            other = BuildId(*other)

        return BuildId.build_compare(self, other) > 0

    def __le__(self, other: BuildIdOrTuple) -> bool:
        if not isinstance(other, BuildId):
            other = BuildId(*other)

        return BuildId.build_compare(self, other) <= 0

    def __ge__(self, other: BuildIdOrTuple) -> bool:
        if not isinstance(other, BuildId):
            other = BuildId(*other)

        return BuildId.build_compare(self, other) >= 0

    def __str__(self) -> str:
        return f"{self.major}.{self.minor}.{self.patch}.{self.build}"


BuildIdOrRange = Union[dbd.build_version, Tuple[dbd.build_version, dbd.build_version]]

@dataclass(init=True, repr=True, frozen=True)
class BuildIdRange:
    min: BuildId
    max: BuildId

    def __contains__(self, item: BuildId) -> bool:
        return self.min <= item <= self.max

    def __str__(self) -> str:
        return f"{self.min}-{self.max}"

    @classmethod
    def from_dbd(cls, build: BuildIdOrRange) -> 'BuildIdRange':
        if isinstance(build, dbd.build_version):
            bb = BuildId.from_dbd(build)
            r = cls(bb, bb)
        elif isinstance(build, tuple):
            bb = BuildId.from_dbd(build[0])
            bbb = BuildId.from_dbd(build[1])
            r = cls(bb, bbb)

        return r



from collections import UserDict, UserList


# a build id dict that you can retrieve from via specific build number.
# works similarly to https://stackoverflow.com/a/39358140/9404062
class DbdBuilds(UserDict[BuildIdRange, 'DbdVersionedCols']):
    # FIXME: consider using 'bisect' for faster lookups
    # (see https://stackoverflow.com/a/39358118/9404062)
    def __contains__(self, key: object) -> bool:
        # if it's a range, behave like a normal dict
        if isinstance(key, BuildIdRange):
            if key in self.data:
                return True
            else:
                return False

        # if a specific version (or a string that might be one), check
        # to see if there's a range
        if isinstance(key, str) or isinstance(key, BuildId):
            try:
                self.__getitem__(key)
                return True
            except KeyError:
                return False

        # else
        return False

    def __getitem__(self, key: object) -> 'DbdVersionedCols':
        # FIXME: wrong
        if isinstance(key, str):
            key = BuildId.from_dbd(dbd.build_version(key))

        # print(f"resolving for key: {key}")
        if isinstance(key, BuildId):
            for k, v in self.data.items():
                # print(f"checking key vs {k} ")
                if key in k:
                    return v

        raise KeyError(key)
        # return None

    @classmethod
    def from_dbd(cls, src: List[dbd.definitions], definitions: 'DbdColumnDefs') -> 'DbdBuilds':
        dbd_builds = cls()

        for dbd_def in src:
            # get our versioned column list built once per def
            c = DbdVersionedCols.from_dbd(dbd_def.entries, definitions)

            # Now add all the builds, pointing at this specific def
            for build in dbd_def.builds:
                b = BuildIdRange.from_dbd(build)
                dbd_builds[b] = c

        # print(f"returning: {dbd_builds}")
        return dbd_builds


@dataclass(init=True, repr=True)
class DbdForeignKey:
    table: str
    column: str


# holds info from dbd_file.columns, which is the global information for
# columns in the database. We'll reference this a bunch when building
# other structures
@dataclass(init=True, repr=True)
class DbdColumnDef:
    name: str
    type: Literal["string", "locstring", "int", "float"]
    is_confirmed_name: bool
    comment: Optional[str] = None
    fk: Optional[DbdForeignKey] = None

    @classmethod
    def from_dbd(cls, src: dbd.column_definition):
        return cls(
            name=src.name,
            type=src.type,
            is_confirmed_name=src.is_confirmed_name,
            comment=src.comment,
            fk=None if not src.foreign else DbdForeignKey(
                table=str(src.foreign.table),
                column=str(src.foreign.column)
            )
        )


class DbdColumnDefs(UserDict[str, DbdColumnDef]):
    @classmethod
    def from_dbd(cls, src: List[dbd.column_definition]) -> 'DbdColumnDefs':
        defs = cls()
        for d in src:
            defs[d.name] = DbdColumnDef.from_dbd(d)

        return defs


@dataclass(init=True, repr=True)
class DbdVersionedCol:
    name: str
    definition: DbdColumnDef
    annotation: Set[str] = dataclasses.field(default_factory=set)
    array_size: Optional[int] = None
    comment: Optional[str] = None
    int_width: Optional[int] = None
    is_unsigned: bool = True

    @classmethod
    def from_dbd(cls, src: dbd.definition_entry, definition: DbdColumnDef) -> 'DbdVersionedCol':
        return cls(
            name=src.column,
            definition=definition,
            annotation=set(src.annotation),
            array_size=src.array_size,
            comment=src.comment,
            int_width=src.int_width,
            is_unsigned=src.is_unsigned
        )


class DbdVersionedCols(UserDict[str, DbdVersionedCol]):
    @classmethod
    def from_dbd(cls, src: List[dbd.definition_entry], definitions: DbdColumnDefs) -> 'DbdVersionedCols':
        cols = cls()
        for d in src:
            cols[d.column] = DbdVersionedCol.from_dbd(d, definitions[d.column])

        return cols


# @dataclass(init=True, repr=True)
# class DbdVersionDef:
#     builds: List[BuildId]
#     comments: List[str] = dataclasses.field(default_factory=list)


# def dbdparse(filename: str) -> None:
#     with open(filename) as file:
#         lines = file.readlines()

#     lines = [line.rstrip() for line in lines]
#     lines = [line for line in lines if line]

#     # print(ppretty(lines))
#     parse_cols(lines)

@dataclass(init=True, repr=True)
class DbdFileData:
    columns: DbdColumnDefs
    definitions: DbdBuilds

    @classmethod
    def from_dbd(cls, src: dbd.dbd_file):
        definitions = DbdColumnDefs.from_dbd(src.columns)
        return cls(
            columns=definitions,
            definitions=DbdBuilds.from_dbd(src.definitions, definitions)
        )

# def dbds(dbd_file: dbd.dbd_file) -> 'DBData':
#     pass


DbdVersionedView = Dict[str, DbdVersionedCols]
class DbdDirectory(UserDict[str, DbdFileData]):
    def get_view(self, build: BuildIdOrTuple) -> DbdVersionedView:
        view: DbdVersionedView = {}
        if isinstance(build, tuple):
            build = BuildId.from_tuple(build)

        for table, tabledef in self.data.items():
            builds = tabledef.definitions
            if build in builds:
                view[table] = builds[build]

        return view

def parse_dbd_file(filename: str) -> DbdFileData:
    dbf = dbd.parse_dbd_file(filename)
    return DbdFileData.from_dbd(dbf)


def parse_dbd_directory(path: str) -> DbdDirectory:
    dbds = DbdDirectory()

    for file in os.listdir(path):
        if file.endswith(".dbd"):
            dbds[file[:-len(".dbd")]] = parse_dbd_file(os.path.join(path, file))

    return dbds

# def get_versioned_view(tables: Dict[str, DbdFileData], build: BuildIdOrTuple) -> Dict[str, DbdVersionedCols]:
#     view: Dict[str, DbdVersionedCols] = {}
#     if isinstance(build, tuple):
#         build = BuildId.from_tuple(build)

#     for table, tabledef in tables.items():
#         builds = tabledef.definitions
#         if build in builds:
#             view[table] = builds[build]

#     return view


if __name__ == "__main__":
    # print(b == b)
    # print(b == (1, 2, 3, 4))
    # print(b < (1, 2, 3, 4))
    # print(b <= (1, 2, 3, 4))
    # print(b < (1, 2, 3, 5))
    # print(b > (1, 2, 3, 5))
    # print(b > (1, 2, 3, 4))
    # print(b >= (1, 2, 3, 4))
    # print(b > (1, 2, 3, 3))

    # c = BuildId(1, 2, 3, 5)
    # print(b == c)
    # print(b < c)

    # print(b == "bob")

    # dbf = dbd.parse_dbd_file("../../defs.mini/Spell.dbd")
    # f = DbdFileData.from_dbd(dbf)
    # f = parse_dbd_file("../../defs.mini/Spell.dbd")
    d = parse_dbd_directory("../../defs.mini")
    b = BuildId(3, 1, 2, 9768)

    v = d.get_view(b)
    print(ppretty(v))

    # for t in d.keys():
    #     print(ppretty(d[t].definitions[b]))

    sys.exit(0)
