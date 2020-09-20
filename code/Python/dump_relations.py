#! /usr/bin/env python3
# -*- coding: utf-8 -*-
import argparse
import dbd
import operator
import os
import sys

import argparse

parser = argparse.ArgumentParser()
parser.add_argument( '--definitions', dest="definitions", type=str, required=True
                   , help="location of .dbd files")
parser.add_argument( '--only', dest="only", action='append'
                   , help='if given, a list of tables to dump')
args = parser.parse_args()

dbds = {}
if args.only:
  for table in args.only:
    dbds[table] = dbd.parse_dbd_file(os.path.join(args.definitions, "{}{}".format(table, dbd.file_suffix)))
else:
  dbds = dbd.parse_dbd_directory(args.definitions)

print ('digraph G {')
print ('rankdir=LR;rank=same;splines=ortho;node[shape=underline]')

needed = {}

for name, parsed in dbds.items():
  for column in parsed.columns:
    if column.foreign:
      t = str(column.foreign.table)
      if t not in needed:
        needed[t] = []
      needed[t] += [str(column.foreign.column)]
      if name not in needed:
        needed[name] = []
      needed[name] += [column.name]

for name, parsed in dbds.items():
  print (u'subgraph "cluster_{}" {{'.format (name))
  print (u'style=filled; color=lightgrey; label="{}"'.format (name))
  columns = {}
  for column in parsed.columns:
    if name in needed:
      if column.name in needed[name]:
        print (u'"{}_{}"'.format (name, column.name))
  print ('}')

for name, parsed in dbds.items():
  for column in parsed.columns:
    if column.foreign:
      print (u'"{}_{}" -> "{}_{}"'.format (name, column.name, column.foreign.table, column.foreign.column))
print ('}')
