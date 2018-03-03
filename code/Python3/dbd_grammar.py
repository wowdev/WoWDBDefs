#! /usr/bin/env python

from modgrammar import *
import modgrammar.debugging
import sys
import logging

class column_type (Grammar):
  grammar = (L("uint") | L("int") | L("string") | L("locstring") | L("float"))

class identifier (Grammar):
  grammar = (WORD("A-Za-z_", restchars="A-Za-z0-9_", fullmatch=True),)

class foreign_identifier (Grammar):
  #! \todo not table is not actually a identifier, but table_naem?
  grammar = (L("<"), identifier, L("::"), identifier, L(">"))

class eol_c_comment (Grammar):
  grammar = OPTIONAL(OPTIONAL(SPACE), L("//"), REST_OF_LINE)

class column_definition (Grammar):
  grammar = ( column_type, OPTIONAL(foreign_identifier)
            , SPACE
            , identifier, OPTIONAL(L("?"))
            , eol_c_comment
            )

class dbd_columns (Grammar):
  grammar = ( L("COLUMNS"), EOL
            , REPEAT(column_definition, EOL)
            )

class integer (Grammar):
  grammar = WORD("0-9")

class build_version (Grammar):
  grammar = (integer, L("."), integer, L("."), integer, L("."), integer)

class column_annotation (Grammar):
  grammar = (L("$"), identifier, L("$"))

class build_entry (Grammar):
  grammar = ( OPTIONAL(column_annotation)
            , identifier
            , OPTIONAL(L("<"), integer, L(">"))
            , OPTIONAL(L("["), integer, L("]"))
            , eol_c_comment
            )

class comma_list_separator (Grammar):
  grammar = (OPTIONAL(SPACE), L(","), OPTIONAL(SPACE))
class build_version_range (Grammar):
  grammar = (build_version, OPTIONAL(L("-"), build_version))
class hex_string (Grammar):
  grammar = WORD("a-fA-F0-9", min=8, max=8, fullmatch=True)
class layout_hash (Grammar):
  grammar = hex_string

class dbd_build_BUILD (Grammar):
  grammar = (L("BUILD"), SPACE, LIST_OF(build_version_range, sep=comma_list_separator))
class dbd_build_LAYOUT (Grammar):
  grammar = (L("LAYOUT"), SPACE, LIST_OF(layout_hash, sep=comma_list_separator))
class dbd_build_COMMENT (Grammar):
  grammar = (L("COMMENT"), REST_OF_LINE)

class dbd_build (Grammar):
  grammar = ( ONE_OR_MORE((dbd_build_BUILD | dbd_build_LAYOUT | dbd_build_COMMENT, EOL))
            , REPEAT(build_entry, EOL)
            )

class dbd_file (Grammar):
  grammar = ( dbd_columns
            , EOL
            , REPEAT(dbd_build, EOF | EOL)
            )

if __name__ == '__main__':
  def read_file(path):
    with open(path) as f:
      return f.read()

  sys.stdout.writelines(generate_ebnf(dbd_file, wrap=None))
  for arg in sys.argv[1:]:
    content = read_file(arg)
    try:
      dbd_parser = dbd_file.parser(debug = True, debug_flags = modgrammar.debugging.DEBUG_ALL | modgrammar.debugging.DEBUG_FULL)
      parsed = dbd_parser.parse_text(content, eof = True)
      print (parsed)
    except Exception as e:
      print (arg)
      print (content)
      raise e