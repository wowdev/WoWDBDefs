local productToExe = {
  wow = 'wow',
  wowt = 'wowt',
  wow_beta = 'wowb',
  wow_classic = 'wowclassic',
  wow_classic_era = 'wowclassic',
  wow_classic_ptr = 'wowclassict',
  wow_classic_era_ptr = 'wowclassict',
}
local args = (function()
  local parser = require('argparse')()
  parser:argument('product', 'product to fetch')
  parser:flag('-v --verbose', 'verbose')
  return parser:parse()
end)()
require('pl.dir').makepath('cache')
local handle = (function()
  local casc = require('casc')
  local bkey, cdn, ckey = casc.cdnbuild('http://us.patch.battle.net:1119/' .. args.product, 'us')
  local handle, err = casc.open({
    bkey = bkey,
    cache = 'cache',
    cacheFiles = true,
    cdn = cdn,
    ckey = ckey,
    locale = casc.locale.US,
    log = args.verbose and print or nil,
    mergeInstall = true,
    zerofillEncryptedChunks = true,
  })
  if not handle then
    print('unable to open casc: ' .. err)
    os.exit(1)
  end
  return handle
end)()
local content = handle:readFile(productToExe[args.product] .. '.exe')
require('pl.file').write(args.product .. '.exe', content)
