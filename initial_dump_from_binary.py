from idautils import *
from idaapi import *
import random
import os

has_extra_info = 1
build="6.0.1.18179"
output_dir = '......../WoWDBDefs/definitions'
retarded_names = True

def definition_to_file (dbc_name, row_size, field_count, field_type, field_size, field_offsets, field_name, field_sql):
  if dbc_name.endswith ("Rec"):
    dbc_name = dbc_name[:-3]
  if dbc_name.endswith ("Rec_C"):
    dbc_name = dbc_name[:-5]

  output = open ('%s/%s.dbd' % (output_dir, dbc_name), 'w+')

  seed = os.urandom(64)

  random.seed (seed)

  print >>output, "COLUMNS"
  offset=0
  for i in range (0, field_count):
    t = Dword (field_type + i * 4)
    field_offset = Dword (field_offsets + i * 4)
    if field_offset != offset:
      padding = field_offset - offset
      print >>output, ("uint padding_%i?" % random.getrandbits(32))
      offset += padding
    size = long (Dword (field_size + i * 4))

    name = GetString (Qword (field_name + i * 8)) if field_name else ("field_%i?" % random.getrandbits(32))
    if retarded_names and name.startswith ("m_"):
      name = name[2:]
    is_lang = False
    if retarded_names and name.endswith ("_lang"):
      name = name[:-5]
      is_lang = True

    type_name = None
    if t == 0 and size % 4 == 0:
      type_name = "uint"
    elif t == 0 and size % 2 == 0:
      type_name = "uint"
    elif (t == 0 and size == 1) or t == 4:
      type_name = "uint"
    elif t == 0 and size == 5:
      type_name = "uint"
    elif t == 1 and size % 8 == 0:
      type_name = "uint"
    elif t == 2 and is_lang:
      type_name = "locstring" if retarded_names else "langstringref"
    elif t == 2:
      type_name = "string" if retarded_names else "stringref"
    elif t == 3 and size % 4 == 0:
      type_name = "float"
    else:
      raise Exception("%s: %i: unknown type %i %i" % (dbc_name, i, t, size))

    print >>output, (type_name + " " + name)
    offset += size
  if row_size != offset:
    print >>output, ("uint padding_%i?" % random.getrandbits(32))

  print >>output, ""

  random.seed (seed)

  print >>output, "BUILD %s" % build
  offset=0
  padd_index=0
  for i in range (0, field_count):
    t = Dword (field_type + i * 4)
    field_offset = Dword (field_offsets + i * 4)
    if field_offset != offset:
      padding = field_offset - offset
      print >>output, ("padding_%i<8>[%i]" % (random.getrandbits(32), padding))
      padd_index += 1
      offset += padding
    size = long (Dword (field_size + i * 4))

    name = GetString (Qword (field_name + i * 8)) if field_name else ("field_%i" % random.getrandbits(32))
    if name.startswith ("m_"):
      name = name[2:]
    if name.endswith ("_lang"):
      name = name[:-5]

    bits = None
    count = 0

    suffix = ""
    if t == 0 and size % 4 == 0:
      bits = 32
      count = size / 4
    elif t == 0 and size % 2 == 0:
      bits = 16
      count = size / 2
    elif (t == 0 and size == 1) or t == 4:
      bits = 8
      count = size
    elif t == 0 and size == 5:
      bits = 8
      count = size
    elif t == 1 and size % 8 == 0:
      bits = 64
      count = size / 8
    elif t == 2:
      count = size / 4
    elif t == 3 and size % 4 == 0:
      count = size / 4
    else:
      raise Exception("%s: %i: unknown type %i %i" % (dbc_name, i, t, size))

    type_string = ""
    if bits:
      type_string += "<%i>" % bits
    if count > 1:
      type_string += "[%i]" % count

    print >>output, (name + type_string)
    offset += size
  if row_size != offset:
    print >>output, ("padding_%i<8>[%i]" % (random.getrandbits(32), (row_size - offset)))

  output.flush()
  output.close()

for position, mangled_name in Names():
  if mangled_name and mangled_name.startswith ("__ZN"):
    if mangled_name.endswith ("6s_metaE"):
      if mangled_name.endswith ("_C6s_metaE"):
        dbc_name = None
        for pos in range (len ("__ZN"), len (mangled_name) - 10):
          if not mangled_name[pos:pos + 1].isdigit():
            dbc_name = mangled_name[pos:len (mangled_name) - 8]
            break
        if not mangled_name.endswith ("_ptr"):
          filename = Qword (position + 0x00)
          if not dbc_name:
            dbc_name = filename
          field_count = Dword (position + 0x10)
          row_size = Dword (position + 0x14)
          field_offset = Qword (position + 0x28)
          field_size = Qword (position + 0x30)
          field_type = Qword (position + 0x38)
          definition_to_file (dbc_name, row_size, field_count, field_type, field_size, field_offset, 0, 0)
      else:
        dbc_name = None
        for pos in range (len ("__ZN"), len (mangled_name) - 11):
          if not mangled_name[pos:pos + 1].isdigit():
            dbc_name = mangled_name[pos:len (mangled_name) - 8]
            break
        if not mangled_name.endswith ("_ptr"):
          filename = Qword (position + 0x00)
          if not dbc_name:
            dbc_name = filename
          field_count = Dword (position + 0x08)
          row_size = Dword (position + 0x0c)
          patch_string_fields_fun = Qword (position + 0x10)
          field_offset = Qword (position + 0x20)
          field_size = Qword (position + 0x28)
          field_type = Qword (position + 0x30)
          field_name = 0
          field_sql = 0
          if has_extra_info:
            field_name = Qword (position + 0x40)
            field_sql = Qword (position + 0x48)
          definition_to_file (dbc_name, row_size, field_count, field_type, field_size, field_offset, field_name, field_sql)

