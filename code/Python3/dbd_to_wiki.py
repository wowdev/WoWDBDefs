#! /usr/bin/env python

import argparse
import dbd
import operator
import os
import sys

def merge_build_ranges(builds):
  out = []

  import itertools
  for version, elements in itertools.groupby(sorted(builds), key=dbd.build_version_raw.version):
    builds = [element.build for element in elements]
    cur = last = builds[0]
    for build in builds[1:]:
      if build - last != 1:
        out += [((version, cur), (version, last) if cur != last else None)]
        cur = build
      last = build
    out += [((version, cur), (version, last) if cur != last else None)]

  return out

import argparse

parser = argparse.ArgumentParser()
parser.add_argument( '--definitions', dest="definitions", type=str, required=True
                   , help="location of .dbd files")
parser.add_argument( '--output', dest="output", type=str, required=True
                   , help="directory to dump wiki pages to")
parser.add_argument( '--only', dest="only", action='append'
                   , help='if given, a list of tables to dump')
args = parser.parse_args()

os.makedirs (args.output, exist_ok=True)

dbds = {}
if args.only:
  for table in args.only:
    dbds[table] = dbd.parse_dbd_file(os.path.join(args.definitions, "{}{}".format(table, dbd.file_suffix)))
else:
  dbds = dbd.parse_dbd_directory(args.definitions)

file_data = {}

for name, parsed in dbds.items():
  file_data[name] = ""

  columns = {}
  for column in parsed.columns:
    columns[column.name] = column
  assert(len(columns)==len(parsed.columns))

  for definition in sorted(parsed.definitions, key=operator.attrgetter('builds')):
    def wiki_format_template(templ, *args):
      templates = { "Type": ("{{{{Type|{}}}}}", "{}ⁱ")
                  , "Unverified": ("{{{{Unverified|{}}}}}", "{}ᵘ")
                  , "ForeignKey": ("{{{{Type/foreign_key|type={}|table={}|column={}}}}}", "foreign_keyⁱ<{}, &{}Rec::{}>")
                  , "SectionBox": ("{{{{SectionBox|{}}}}}", "WRONG {} WRONG")
                  , "PrettyVersion": ("{{{{Sandbox/PrettyVersion|{}}}}}", "WRONG {} WRONG")
                  , "VersionRange": ("{{{{Sandbox/VersionRange|{}|{}}}}}", "WRONG {}|{} WRONG")
                  }
      sf, lf = templates[templ]
      s = sf.format(*args)
      l = len(lf.format(*args))
      return (s, l)
    def wiki_format_raw(fmt, *args):
      s = fmt.format(*args)
      return (s, len(s))

    lines = []
    for entry in definition.entries:
      meta = columns[entry.column]

      #! \todo annotations

      def wiki_format_type():
        print (meta.type, entry.column, name)
        if meta.type in ["uint", "int"]:
          type = wiki_format_raw("{}{}_t", meta.type if not entry.is_unsigned else "uint", entry.int_width if entry.int_width else 32)
          if meta.foreign:
            return wiki_format_template("ForeignKey", type[0], meta.foreign.table, "m_{}".format(meta.foreign.column))
          return type
        assert (not entry.int_width)
        assert (not meta.foreign)

        if meta.type in ["string", "locstring"]:
          wikiname = "stringref" if meta.type == "string" else "langstringref"
          return wiki_format_template("Type", wikiname)
        else:
          return wiki_format_raw("{}", meta.type)
      type_str = wiki_format_type()

      array_str_str = "[{}]".format(entry.array_size) if entry.array_size else ""
      array_str = (array_str_str, len(array_str_str))

      if meta.is_confirmed_name:
        name_str = wiki_format_raw("m_{}".format(entry.column))
      else:
        name_str = wiki_format_template("Unverified", "m_{}".format(entry.column))

      comments = []

      merged_str_pattern = "   {} {}{};"
      for annotation in entry.annotation:
        if annotation == "noninline":
          merged_str_pattern = "   // {} {}{};"
          comments += ["non-inline field"]
        elif annotation == "id":
          pass
        else:
          comments += ["{}".format(annotation)]
      merged_str = merged_str_pattern.format(type_str[0], name_str[0], array_str[0])
      merged_str_visual_len = len(merged_str_pattern.format('t'*type_str[1], 'n'*name_str[1], 'a'*array_str[1]))

      comments += [entry.comment] if entry.comment else []
      comments += [meta.comment] if meta.comment else []

      lines += [(merged_str, merged_str_visual_len, comments)]

    comment_indent = max(lines, key=operator.itemgetter(1))[1] + 2

    build_ranges = merge_build_ranges(definition.builds)
    multiple_builds = 0
    section_title_builds = []
    for begin, end in build_ranges:
      if not end:
        section_title_builds += ["{}.{}".format(begin[0], begin[1])]
        multiple_builds += 1
      else:
        section_title_builds += ["{}.{}-{}.{}".format(begin[0], begin[1], end[0], end[1])]
        multiple_builds += 2
    build_ranges_str = ', '.join(section_title_builds)

    layout_hashes = [str(layout) for layout in definition.layouts]
    #! \todo This is a really shit section title.
    file_data[name] += "=={}==\n".format(", ".join(section_title_builds + layout_hashes))

    box_content = "This definition applies to "
    def wiki_format_version(version, build, prefix = ''):
      #! \todo will break with version 10.
      return "{}expansionlevel={}|{}build={}.{}".format(prefix, version[0], prefix, version, build)
    if multiple_builds == 1:
      build = build_ranges[0][0]
      box_content += "version {}".format(wiki_format_template("PrettyVersion", wiki_format_version(build[0], build[1]))[0])
    elif multiple_builds > 1:
      box_content += "versions \n* "
      strs = []
      for begin, end in build_ranges:
        if not end:
          strs += [wiki_format_template("PrettyVersion", wiki_format_version(begin[0], begin[1]))[0]]
        else:
          strs += [wiki_format_template("VersionRange", wiki_format_version(begin[0], begin[1], "min_"), wiki_format_version(end[0], end[1], "max_"))[0]]
      box_content += "\n* ".join (strs)
    if layout_hashes:
      if multiple_builds:
        box_content += "{}and ".format(" " if multiple_builds == 1 else "\n")

      if len(layout_hashes) == 1:
        box_content += "layout hash <tt>{}</tt>".format(layout_hashes[0])
      elif len(layout_hashes) > 1:
        box_content += "layout hashes \n* <tt>{}</tt>".format("</tt>\n* <tt>".join(layout_hashes))
    file_data[name] += wiki_format_template("SectionBox", box_content)[0] + "\n"

    for comment in definition.comments:
      file_data[name] += str(comment) + "\n\n"
    file_data[name] += " struct {}Rec {{\n".format(name)
    for line, linelen, comments in lines:
      if comments:
        file_data[name] += "{}{} // {}\n".format(line, ' '*(comment_indent - linelen), comments[0])
        for comment in comments[1:]:
          file_data[name] += "{} // {}\n".format(" "*comment_indent, comment)
      else:
        file_data[name] += line + "\n"
    file_data[name] += " };\n"

for name, data in file_data.items():
  print(name)
  with open(os.path.join(args.output, "{}.mwiki".format(name)), "w") as f:
    f.write(data)
