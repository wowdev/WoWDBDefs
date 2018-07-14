# coding: utf-8

# - an enum-style thing name for identifier → name. in fact the name is purely
#   descriptive, just the identifiers shall be unique (python doesn’t have
#   enums…)
# - a class for patterns pattern which has
#   - a name name
#   - a position to keep track for building the pattern cur_pos
#   - the pattern itself as a string cur_pattern
#   - a map of name to offsets offsets
#   - functions to append to the pattern
#     - append name → cur_pos to offsets
#     - padd with 00 until the current position matches the alignment requiements padd_to
#     - increases the current position by the number of bytes append
#     - appends the pattern itself (also append )
#    - helper functions for the types before (the pattern__ variables essentially, but with a name)
# - the matching itself no longer just matches and reads with hardcoded
#   offsets but instead uses the current pattern’s offsets[name.XXX] to
#   access. by checking if that name is in the map to begin with, it is
#   version independent (e.g. name.FDID )

def read_uint8_t (ea): return Byte (ea)
def read_uint32_t (ea): return Dword (ea)
def read_string (ea): return GetString (ea)
def find_next (start, pattern):
  res = find_binary (ea + 1, SEARCH_DOWN, pattern)
  return None if res == idaapi.BADADDR else res

class pattern:
  def __init__(self, name):
    self.name = name
    self.cur_pos = 0
    self.cur_pattern = ""
    # todo: probably also type/size for cross platform...
    self.offsets = {}

  def append(self, *args):
    for what in args:
      self.cur_pattern += what + " "
      self.cur_pos += 1
    return self
  def padd_to(self, align):
    while self.cur_pos % align:
      self.append ("00")
    return self
  def remember(self, name):
    self.offsets[name] = self.cur_pos

  def pointer(self, name):
    self.padd_to (8)
    self.remember (name)
    self.append ("?", "?", "?", "?", "01", "00", "00", "00")
    return self
  def optional_pointer(self, name):
    self.padd_to (8)
    self.remember (name)
    self.append ("?", "?", "?", "?", "?", "00", "00", "00")
    return self
  def filedataid(self, name):
    self.padd_to (4)
    self.remember (name)
    self.append ("?", "?", "?", "?")
    return self
  def field_reference(self, name):
    self.padd_to (4)
    self.remember (name)
    self.append ("?", "?", "00", "00")
    return self
  def optional_field_reference(self, name):
    self.padd_to (4)
    self.remember (name)
    self.append ("?", "?", "?", "?")
    return self
  def record_size(self, name):
    return self.field_reference(name)
  def uint8(self, name):
    self.padd_to (1)
    self.remember (name)
    self.append ("?")
    return self
  def boolean(self, name):
    return self.uint8 (name)
  def hash(self, name):
    self.padd_to (4)
    self.remember (name)
    self.append ("?", "?", "?", "?")
    return self

class name:
    DB_NAME = "db name"
    DBC_FILENAME = "dbc file name"
    DB2_FILENAME = "db2 file name"
    DB_ADB_FILENAME = "db adb file name"
    FDID = "fdid"
    NUM_FIELD_IN_FILE = "fields in file"
    RECORD_SIZE = "record size"
    NUM_FIELD = "fields"
    ID_COLUMN = "id column"
    SPARSE_TABLE = "sparse table"
    FIELD_DEFAULTS = "defaults"
    FIELD_OFFSETS = "offsets"
    FIELD_SIZES = "sizes"
    FIELD_TYPES = "types"
    FIELD_FLAGS = "flags"
    FIELD_SIZES_IN_FILE = "sizes in file"
    FIELD_TYPES_IN_FILE = "types in file"
    FIELD_FLAGS_IN_FILE = "flags in file"
    FIELD_NAMES_IN_FILE = "names in file"
    FLAGS_58_21 = "flags 58: 2|1"
    TABLE_HASH = "table"
    SIBLING_TABLE_HASH = "the sparse, or non-sparse equivalent"
    LAYOUT_HASH = "layout"
    FLAGS_68_421 = "flags 68: 4|2|1"
    FIELD_NUM_IDX_INT = "nbUniqueIdxByInt"
    FIELD_NUM_IDX_STRING = "nbUniqueIdxByString"
    FIELD_IDX_INT = "uniqueIdxByInt"
    FIELD_IDX_STRING = "uniqueIdxByString"
    UNK88 = "unk88"
    FIELD_RELATION = "relation"
    FIELD_RELATION_IN_FILE = "relation in file"
    SORT_FUNC = "sort function"
    UNKC0 = "unkC0"
    CONVERT_STRINGREFS = "convert stringrefs"
    FIELD_ENCRYPTED = "encrypted"
    SQL_QUERY = "sql query"
    UNK_BOOL_601_x24 = "unknown bool, always true"
    UNK_FLAGS_601_x48_421 = "possibly flags: 4|2|1" # todo is this FLAGS_68_421?
    UNK_BOOL_601dbc_x38 = "unkown bool x38 6.0.1"
    UNK_BOOL_601dbc_x39 = "unkown bool x39 6.0.1"
    UNK_BOOL_601dbc_x3a = "unkown bool x3a 6.0.1, always false"
    UNK_BOOL_601dbc_x3b = "unkown bool x3b 6.0.1"

patterns = \
[ pattern("release-8.0.1")
   .pointer (name.DB_NAME)
   .filedataid (name.FDID)
   .field_reference (name.NUM_FIELD_IN_FILE)
   .record_size (name.RECORD_SIZE)
   .field_reference (name.NUM_FIELD)
   .optional_field_reference (name.ID_COLUMN)
   .boolean (name.SPARSE_TABLE)
   .pointer (name.FIELD_OFFSETS)
   .pointer (name.FIELD_SIZES)
   .pointer (name.FIELD_TYPES)
   .pointer (name.FIELD_FLAGS)
   .pointer (name.FIELD_SIZES_IN_FILE)
   .pointer (name.FIELD_TYPES_IN_FILE)
   .pointer (name.FIELD_FLAGS_IN_FILE)
   .uint8 (name.FLAGS_58_21)
   .hash (name.TABLE_HASH)
   .append ("00", "00", "00", "00")
   .hash (name.LAYOUT_HASH)
   .uint8 (name.FLAGS_68_421)
   .field_reference (name.FIELD_NUM_IDX_INT)
   .field_reference (name.FIELD_NUM_IDX_STRING)
   .optional_pointer (name.FIELD_IDX_INT)
   .optional_pointer (name.FIELD_IDX_STRING)
   .boolean (name.UNK88)
   .optional_field_reference (name.FIELD_RELATION)
   .optional_field_reference (name.FIELD_RELATION_IN_FILE)
   .optional_pointer (name.SORT_FUNC)
   .boolean (name.UNKC0)
, pattern("release-7.3.5")
   .pointer (name.DB_NAME)
   .field_reference (name.NUM_FIELD_IN_FILE)
   .record_size (name.RECORD_SIZE)
   .field_reference (name.NUM_FIELD)
   .optional_field_reference (name.ID_COLUMN)
   .boolean (name.SPARSE_TABLE)
   .pointer (name.FIELD_OFFSETS)
   .pointer (name.FIELD_SIZES)
   .pointer (name.FIELD_TYPES)
   .pointer (name.FIELD_FLAGS)
   .pointer (name.FIELD_SIZES_IN_FILE)
   .pointer (name.FIELD_TYPES_IN_FILE)
   .pointer (name.FIELD_FLAGS_IN_FILE)
   .uint8 (name.FLAGS_58_21)
   .hash (name.TABLE_HASH)
   .append ("00", "00", "00", "00")
   .hash (name.LAYOUT_HASH)
   .uint8 (name.FLAGS_68_421)
   .field_reference (name.FIELD_NUM_IDX_INT)
   .field_reference (name.FIELD_NUM_IDX_STRING)
   .optional_pointer (name.FIELD_IDX_INT)
   .optional_pointer (name.FIELD_IDX_STRING)
   .boolean (name.UNK88)
   .optional_field_reference (name.FIELD_RELATION)
   .optional_field_reference (name.FIELD_RELATION_IN_FILE)
   .optional_pointer (name.SORT_FUNC)
   .boolean (name.UNKC0)
, pattern("ptr-7.3.5")
   .pointer (name.DB_NAME)
   .field_reference (name.NUM_FIELD_IN_FILE)
   .record_size (name.RECORD_SIZE)
   .field_reference (name.NUM_FIELD)
   .optional_field_reference (name.ID_COLUMN)
   .boolean (name.SPARSE_TABLE)
   .pointer (name.FIELD_OFFSETS)
   .pointer (name.FIELD_SIZES)
   .pointer (name.FIELD_TYPES)
   .pointer (name.FIELD_SIZES_IN_FILE)
   .pointer (name.FIELD_TYPES_IN_FILE)
   .pointer (name.FIELD_FLAGS_IN_FILE)
   .uint8 (name.FLAGS_58_21)
   .hash (name.TABLE_HASH)
   .append ("00", "00", "00", "00")
   .hash (name.LAYOUT_HASH)
   .uint8 (name.FLAGS_68_421)
   .field_reference (name.FIELD_NUM_IDX_INT)
   .field_reference (name.FIELD_NUM_IDX_STRING)
   .optional_pointer (name.FIELD_IDX_INT)
   .optional_pointer (name.FIELD_IDX_STRING)
   .boolean (name.UNK88)
   .optional_field_reference (name.FIELD_RELATION)
   .optional_field_reference (name.FIELD_RELATION_IN_FILE)
   .optional_pointer (name.SORT_FUNC)
   .boolean (name.UNKC0)
, pattern("release-7.{2.5,3.{0,2}}") # note: also matches release-7.3.5 even though different struct
   .pointer (name.DB_NAME)
   .field_reference (name.NUM_FIELD_IN_FILE)
   .record_size (name.RECORD_SIZE)
   .field_reference (name.NUM_FIELD)
   .optional_field_reference (name.ID_COLUMN)
   .boolean (name.SPARSE_TABLE)
   .pointer (name.FIELD_DEFAULTS) ## not sure which versions, yay
   .pointer (name.FIELD_OFFSETS)
   .pointer (name.FIELD_SIZES)
   .pointer (name.FIELD_TYPES)
   .pointer (name.FIELD_SIZES_IN_FILE)
   .pointer (name.FIELD_TYPES_IN_FILE)
   .pointer (name.FIELD_FLAGS_IN_FILE)
   .uint8 (name.FLAGS_58_21)
   .hash (name.TABLE_HASH)
   .append ("00", "00", "00", "00")
   .hash (name.LAYOUT_HASH)
   .uint8 (name.FLAGS_68_421)
   .field_reference (name.FIELD_NUM_IDX_INT)
   .field_reference (name.FIELD_NUM_IDX_STRING)
   .optional_pointer (name.FIELD_IDX_INT)
   .optional_pointer (name.FIELD_IDX_STRING)
   .optional_field_reference (name.FIELD_RELATION)
   .optional_field_reference (name.FIELD_RELATION_IN_FILE)
   .optional_pointer (name.SORT_FUNC)
   .boolean (name.UNKC0)
, pattern("internal-6.0.1-db2") # note: conflicts with internal-6.0.1-dbc
   .pointer (name.DB2_FILENAME)
   .pointer (name.DB_ADB_FILENAME)
   .field_reference (name.NUM_FIELD_IN_FILE)
   .record_size (name.RECORD_SIZE)
   .pointer (name.CONVERT_STRINGREFS)
   .append ("00", "00", "00", "00") # 20
   .boolean (name.UNK_BOOL_601_x24)
   .boolean (name.SPARSE_TABLE)
   .pointer (name.FIELD_OFFSETS)
   .pointer (name.FIELD_SIZES_IN_FILE)
   .pointer (name.FIELD_TYPES_IN_FILE)
   .hash (name.TABLE_HASH)
   .hash (name.SIBLING_TABLE_HASH)
   .uint8 (name.UNK_FLAGS_601_x48_421)
   .field_reference (name.FIELD_NUM_IDX_INT)
   .field_reference (name.FIELD_NUM_IDX_STRING)
   .optional_pointer (name.FIELD_IDX_INT)
   .optional_pointer (name.FIELD_IDX_STRING)
   .append ("?", "?", "?", "?") # 68 probably not a column, only 0 or 1
   .append ("?", "?", "?", "?") # 6c
   .pointer (name.FIELD_ENCRYPTED) # 70
, pattern("internal-6.0.1-dbc") # note: conflicts with internal-6.0.1-db2
   .pointer (name.DBC_FILENAME)
   .field_reference (name.NUM_FIELD_IN_FILE)
   .record_size (name.RECORD_SIZE)
   .pointer (name.CONVERT_STRINGREFS)
   .append ("00", "00", "00", "00", "00", "00", "00", "00")
   .pointer (name.FIELD_OFFSETS)
   .pointer (name.FIELD_SIZES_IN_FILE)
   .pointer (name.FIELD_TYPES_IN_FILE)
   .boolean (name.UNK_BOOL_601dbc_x38)
   .boolean (name.UNK_BOOL_601dbc_x39)
   .boolean (name.UNK_BOOL_601dbc_x3a)
   .boolean (name.UNK_BOOL_601dbc_x3b)
   .append ("00", "00", "00", "00")
   .pointer (name.FIELD_NAMES_IN_FILE)
   .pointer (name.SQL_QUERY)
, pattern("internal-5.0.1-dbc") # note: subset of internal-6.0.1-dbc, so conflicts with that
   .pointer (name.DBC_FILENAME)
   .field_reference (name.NUM_FIELD_IN_FILE)
   .record_size (name.RECORD_SIZE)
   .pointer (name.CONVERT_STRINGREFS)
   .append ("?", "?", "?", "?")
   .append ("?", "?", "?", "?")
   .pointer (name.FIELD_OFFSETS)
   .pointer (name.FIELD_SIZES_IN_FILE)
   .pointer (name.FIELD_TYPES_IN_FILE)
, pattern("internal-5.0.1-db2") # note: subset of internal-6.0.1-db2, so conflicts with that and internal-5.0.1-dbc
   .pointer (name.DB2_FILENAME)
   .pointer (name.DB_ADB_FILENAME)
   .field_reference (name.NUM_FIELD_IN_FILE)
   .record_size (name.RECORD_SIZE)
   .pointer (name.CONVERT_STRINGREFS)
   .append ("?", "?", "?", "?")
   .append ("?", "?", "?", "?")
   .pointer (name.FIELD_OFFSETS)
   .pointer (name.FIELD_SIZES_IN_FILE)
   .pointer (name.FIELD_TYPES_IN_FILE)
   .hash (name.TABLE_HASH)
]

matchcount = 0

for pattern in patterns:
  ea = -1
  while True:
    ea = find_next (ea, pattern.cur_pattern)
    if not ea: break

    def is_insane_fdid(name_):
      # file data id is probably newer than when they added fdids
      return name_ in pattern.offsets and read_uint32_t (ea + pattern.offsets[name_]) < 53183
    def is_insane_record_size(name_):
      # record_size = 0 lol
      return name_ in pattern.offsets and read_uint32_t (ea + pattern.offsets[name_]) == 0
    def is_insane_pointer(name_):
      # what a bad lower part of a 64 bit pointer that would be
      return name_ in pattern.offsets and read_uint32_t (ea + pattern.offsets[name_]) < 10
    def is_bad_filename_pointer(name_, suffix_):
      if not name_ in pattern.offsets:
        return False
      filename = read_string (Qword (ea + pattern.offsets[name_]))
      return not filename or filename[-len(suffix_):] != suffix_

    if is_insane_fdid(name.FDID): continue
    if is_insane_record_size(name.RECORD_SIZE): continue
    if is_insane_pointer(name.DB_NAME): continue
    if is_insane_pointer(name.CONVERT_STRINGREFS): continue
    if is_bad_filename_pointer(name.DB_ADB_FILENAME, "adb"): continue
    if is_bad_filename_pointer(name.DBC_FILENAME, "dbc"): continue
    if is_bad_filename_pointer(name.DB2_FILENAME, "db2"): continue

    maybe_name = "no DB_NAME in struct?!"
    if name.DB_NAME in pattern.offsets:
      maybe_name = read_string(Qword(ea + pattern.offsets[name.DB_NAME]))
    elif name.DB2_FILENAME in pattern.offsets:
      maybe_name = read_string(Qword(ea + pattern.offsets[name.DB2_FILENAME]))
    elif name.DBC_FILENAME in pattern.offsets:
      maybe_name = read_string(Qword(ea + pattern.offsets[name.DBC_FILENAME]))
    print (pattern.name, hex(ea), maybe_name)

    matchcount += 1

print ("matchcount", matchcount)
