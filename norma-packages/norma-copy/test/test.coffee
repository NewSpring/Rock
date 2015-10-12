Path = require "path"
Fs = require "fs"
Chai = require("chai").should()

Norma = require "normajs"

describe 'Packages', ->

  homePath = Path.resolve "./"
  packageJsonPath = Path.resolve homePath, "package.json"

  describe "install process", ->

    it "find norma-copy", (done) ->

      # set massive timeout incase npmjs or github are slow
      @.timeout 100000
      Norma.getPackages()
        .then( (packages) ->

          packages.should
            .contain.any.keys["copy"]

          done()
        ).fail( (err) ->
          console.log err
        )
