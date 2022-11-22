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
local exesToTry = {
  'Wow.exe',
  'WowT.exe',
  'WowB.exe',
  'WowClassic.exe',
  'WowClassicT.exe',
  'WowClassicB.exe',
  'Wow-64.exe',
  'WowT-64.exe',
  'WowB-64.exe',
}
local content = (function()
  for _, exe in ipairs(exesToTry) do
    local content = handle:readFile(exe)
    if content then
      if args.verbose then
        print('fetching ' .. exe)
      end
      return content
    end
  end
  error('could not find executable')
end)()
require('pl.file').write(args.product .. '.exe', content)
