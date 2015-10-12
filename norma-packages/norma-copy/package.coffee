Path        = require "path"
Norma       = require "normajs"
Plumber     = require "gulp-plumber"
Rename      = require "gulp-rename"
If          = require "gulp-if"


module.exports = (config, name) ->

  # console.log config.tasks[name]
  # CONFIG ----------------------------------------------------------------
  if !name then name = "copy"

  if !config.tasks[name]
    return

  options = config.tasks[name]

  src = Path.normalize(options.src)
  dest = Path.normalize(options.dest)


  # order
  if options.order
    order = options.order
  else
    order = "post"

  # ext
  if !options.ext
    options.ext = "*"

  if typeof options.ext is "string"
    options.ext = [options.ext]


  extType = options.ext.map( (ext) ->
    return ext.replace(".", "").trim()
  )


  # #{name}-COMPILE ----------------------------------------------------------

  Norma.task "#{name}-compile", (cb) ->

    if extType.length > 1
      extensions  = "{#{extType.join(",")}}"
    else
      extensions = extType

    # Make sure other files and folders are copied over
    Norma.src([
      src + ".#{extensions}",
    ])
      .pipe Plumber()
      .pipe If(options.base, Rename((path) ->
        parts = path.dirname.split("/")
        parts.splice(0, 1)

        path.dirname = parts.join("/")
      ))
      .pipe Norma.dest(dest)
      .on "error", (err) ->
        error =
          message: err
          level: "crash"
        Norma.error error
      .on "close", ->
        cb null


  # COPY ------------------------------------------------------------------

  Norma.task "#{name}", (cb, tasks) ->

    Norma.execute "#{name}-compile", ->

      Norma.log "#{name}: ✔ All done!"


      cb null
      return



  if options.type
    Norma.tasks["#{name}"].type = options.type

  Norma.tasks["#{name}"].order = order
  Norma.tasks["#{name}"].ext = options.ext

  module.exports.tasks = Norma.tasks
