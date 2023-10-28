rockspec_format = '3.0'
package = 'getexe'
version = 'scm-0'
source = { url = '' }
dependencies = {
  'argparse',
  'luacasc < 1.16',
  'penlight',
}
build = {
  type = 'none',
  install = {
    bin = {
      getexe = 'getexe.lua',
    },
  },
}
