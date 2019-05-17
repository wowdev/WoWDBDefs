#! /usr/bin/env python

from modgrammar import *
from itertools import chain
import os

# parses a given dbd (as string) and returns an object with
#
# .columns[]
#     .type: string
#     .name: string
#     .is_confirmed_name: bool
#     .foreign
#         .table: string
#         .column: string
#     .comment: string
# .definitions[]
#     .builds[]: build_version_raw (single build), build_version_raw[2] (build range)
#         .major: int
#         .minor : int
#         .patch : int
#         .build : int
#     .layouts[]: string
#     .comments[]: string
#     .entries[]:
#         .column: string
#         .int_width: int
#         .is_unsigned: bool
#         .array_size: int
#         .annotation: string
#         .comment: string


def parse_dbd(content):
    return dbd_parser.parse_string(content)


def parse_dbd_file(path):
    with open(path) as f:
        return parse_dbd(f.read())


file_suffix = ".dbd"


def parse_dbd_directory(path):
    dbds = {}
    for file in os.listdir(path):
        if file.endswith(file_suffix):
            dbds[file[:-len(file_suffix)]] = parse_dbd_file(os.path.join(path,file))
    return dbds


class column_type(Grammar):
    grammar = L("uint") | L("int") | L("string") | L("locstring") | L("float")
    grammar_collapse = True


class identifier(Grammar):
    grammar = WORD("A-Za-z_", restchars="A-Za-z0-9_", fullmatch=True)
    grammar_collapse = True


class integer(Grammar):
    grammar = WORD("0-9", fullmatch=True)
    grammar_collapse = True


class layout_hash(Grammar):
    grammar = WORD("a-fA-F0-9", min=8, max=8, fullmatch=True)
    grammar_collapse = True


class eol_c_comment(Grammar):
    grammar = (OPTIONAL(SPACE, collapse_skip=True), L("//"), REST_OF_LINE)
    grammar_collapse = True


class comma_list_separator(Grammar):
    grammar = (OPTIONAL(SPACE), L(","), OPTIONAL(SPACE))
    grammar_noteworthy = False
    grammar_collapse_skip = True


class foreign_identifier (Grammar):
    #! \todo table is not actually a identifier, but table_name?
    grammar = (L("<"), identifier, L("::"), identifier, L(">"))

    def grammar_elem_init(self, sessiondata):
        self.table = self.elements[1]
        self.column = self.elements[3]

    def __str__(self):
        return "{}::{}".format(self.table, self.column)


class column_definition (Grammar):
    grammar = ( column_type, OPTIONAL(foreign_identifier)
                        , SPACE
                        , G(identifier, name="column_name"), OPTIONAL(L("?"))
                        , OPTIONAL(eol_c_comment)
                        )

    def grammar_elem_init(self, sessiondata):
        self.type = str(self.elements[0])
        self.foreign = self.elements[1]
        self.name = str(self.elements[3])
        self.is_confirmed_name = not self.elements[4]
        self.comment = str(self.elements[5]).strip() if self.elements[5] else None

    def __str__(self):
        return "type={} fk={} name={} confirmed={} comment={}".format(self.type, self.foreign, self.name, self.is_confirmed_name, self.comment)


class build_version (Grammar):
    grammar = (integer, L("."), integer, L("."), integer, L("."), integer)

    def grammar_elem_init(self, sessiondata):
        self.major = int(str(self.elements[0]))
        self.minor = int(str(self.elements[2]))
        self.patch = int(str(self.elements[4]))
        self.build = int(str(self.elements[6]))

    def __str__(self):
        return "{}.{}.{}.{}".format(self.major, self.minor, self.patch, self.build)


class build_version_raw:
    def __init__(self, major, minor, patch, build):
        self.major = major
        self.minor = minor
        self.patch = patch
        self.build = build

    def __str__(self):
        return "{}.{}".format (self.version(), self.build)

    def version(self):
        return "{}.{}.{}".format(self.major, self.minor, self.patch)

    def __lt__(self, rhs):
        return (self.major, self.minor, self.patch, self.build) \
                 < (rhs.major, rhs.minor, rhs.patch, rhs.build)

    def __le__(self, rhs):
        return (self.major, self.minor, self.patch, self.build) \
               <= (rhs.major, rhs.minor, rhs.patch, rhs.build)

    def __eq__(self, rhs):
        return (self.major, self.minor, self.patch, self.build) \
               == (rhs.major, rhs.minor, rhs.patch, rhs.build)

    def __ne__(self, rhs):
        return (self.major, self.minor, self.patch, self.build) \
               != (rhs.major, rhs.minor, rhs.patch, rhs.build)

    def __gt__(self, rhs):
        return (self.major, self.minor, self.patch, self.build) \
               > (rhs.major, rhs.minor, rhs.patch, rhs.build)

    def __ge__(self, rhs):
        return (self.major, self.minor, self.patch, self.build) \
               >= (rhs.major, rhs.minor, rhs.patch, rhs.build)


class build_version_range(Grammar):
    grammar = (build_version, OPTIONAL(L("-"), build_version))

    def grammar_elem_init(self, sessiondata):
        lhs = self.elements[0]
        rhs = self.elements[1][1] if self.elements[1] else None

        self.builds = (build_version_raw(lhs.major, lhs.minor, lhs.patch, lhs.build),
                       build_version_raw(rhs.major, rhs.minor, rhs.patch, rhs.build)) if rhs else lhs


class definition_BUILD(Grammar):
    grammar = (L("BUILD"),
               SPACE,
               LIST_OF(build_version_range, sep=comma_list_separator, collapse=True)
               )

    def grammar_elem_init(self, sessiondata):
        self.builds = [ranges.builds for ranges in self.elements[2:]]

    grammar_tags = ["BUILD"]


class definition_LAYOUT(Grammar):
    grammar = (L("LAYOUT"),
               SPACE,
               LIST_OF(layout_hash, sep=comma_list_separator, collapse=True)
               )

    def grammar_elem_init(self, sessiondata):
        self.layouts = [str(layout) for layout in self.elements[2:]]

    grammar_tags = ["LAYOUT"]


class definition_COMMENT(Grammar):
    grammar = (L("COMMENT"),
               SPACE,
               G(REST_OF_LINE, tags=["COMMENT"]))

    grammar_collapse = True


class definition_entry(Grammar):
    grammar = (OPTIONAL(G(L("$"), G(LIST_OF(G(identifier, tags=["ANNOTATION"], sep=comma_list_separator), name="annotation", collapse=True), L("$"), collapse=True))),
               G(identifier, name="column_name"),
               OPTIONAL(G(L("<"), G(OPTIONAL(L("u")), integer, name="int_width"), L(">"), collapse=True)),
               OPTIONAL(G(L("["), G(integer, name="array_size"), L("]"), collapse=True)),
               OPTIONAL(eol_c_comment)
              )

    def grammar_elem_init(self, sessiondata):
        self.annotation = [str(e) for e in self.elements[0].find_all("ANNOTATION")] if self.elements[0] else []
        self.column = str(self.elements[1]) if self.elements[1] else None
        self.is_unsigned = False
        self.int_width = None

        if self.elements[2]:
            int_width = str(self.elements[2])
            if int_width[0:1] == u"u":
                int_width = int_width[1:]
                self.is_unsigned = True
            self.int_width = int(int_width)

        self.array_size = int(str(self.elements[3])) if self.elements[3] else None
        self.comment = str(self.elements[4]).strip() if self.elements[4] else None

    def __str__(self):
        return "column={} int_width={} array_size={} annotation={} comment={}".format(self.column, self.int_width, self.array_size, self.annotation, self.comment)

    grammar_tags = ["ENTRY"]


class definition(Grammar):
    grammar = (ONE_OR_MORE(G(definition_BUILD | definition_LAYOUT | definition_COMMENT, EOL, name="definition_header")),
               REPEAT(definition_entry, EOL)
              )

    def grammar_elem_init(self, sessiondata):

        def flatten(lis, rec_depth=0):
            from collections import Iterable
            for item in lis:
                if isinstance(item, Iterable) and not isinstance(item, str):
                    for x in flatten(item):
                        yield x
                else:
                    yield item

        self.builds = list(chain.from_iterable(([builds.builds for builds in self.find_all("BUILD")])))
        # self.builds = list(flatten([builds.builds for builds in self.find_all("BUILD")]))
        self.layouts = list(flatten([layouts.layouts for layouts in self.find_all("LAYOUT")]))
        self.comments = [str(comment) for comment in self.find_all("COMMENT")]
        self.entries = [entry for entry in self.find_all("ENTRY")]


class dbd_file(Grammar):
    grammar = (L("COLUMNS"),
               EOL,
               REPEAT(column_definition, EOL),
               EOL,
               REPEAT(definition, EOF | EOL)
               )

    def grammar_elem_init(self, sessiondata):
        self.columns = [x[0] for x in self.elements[2]]
        self.definitions = [x[0] for x in self.elements[4]]


dbd_parser = dbd_file.parser()
