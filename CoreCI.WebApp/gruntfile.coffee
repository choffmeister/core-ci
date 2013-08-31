###
  Copyright (C) 2013 Christian Hoffmeister

  This program is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program. If not, see {http://www.gnu.org/licenses/}.
###
path = require("path")
send = require("send")
mountFolder = (connect, dir) ->
  connect.static path.resolve(dir)

module.exports = (grunt) ->
  grunt.initConfig
    requirejs:
      prod:
        options:
          baseUrl: "target/dev/src"
          mainConfigFile: "target/dev/src/app.js"
          name: "app"
          out: "target/prod/src/app.js"
          optimize: "uglify"

    coffee:
      dev:
        expand: true
        cwd: "src"
        src: ["**/*.coffee"]
        dest: "target/dev/src"
        ext: ".js"
        options:
          bare: true

    jade:
      dev:
        files: [
          expand: true
          cwd: "resources"
          src: "**/*.jade"
          dest: "target/dev"
          ext: ".html"
          rename: (dest, src) ->
            path.join dest, (if src is "index.html" then "_index.html" else src)
        ]
        options:
          pretty: true

      prod:
        files: [
          expand: true
          cwd: "resources"
          src: "**/*.jade"
          dest: "target/prod"
          ext: ".html"
        ]
        options:
          pretty: false

    less:
      dev:
        files: [
          src: "resources/styles/main.less"
          dest: "target/dev/styles/main.css"
        ]
        options:
          paths: ["resources/styles"]
          yuicompress: false

      prod:
        files: [
          src: "resources/styles/main.less"
          dest: "target/prod/styles/main.css"
        ]
        options:
          paths: ["resources/styles"]
          yuicompress: true

    copy:
      dev:
        files: [
          src: "resources/favicon.ico"
          dest: "target/dev/favicon.ico"
        ,
          expand: true
          cwd: "resources/images"
          src: "**/*.*"
          dest: "target/dev/images"
        ]

      prod:
        files: [
          expand: true
          cwd: "bower_components"
          src: "**/*"
          dest: "target/dev/bower_components"
        ,
          expand: true
          cwd: "bower_components"
          src: "**/*"
          dest: "target/prod/bower_components"
        ,
          src: "resources/favicon.ico"
          dest: "target/prod/favicon.ico"
        ,
          src: "resources/robots.txt"
          dest: "target/prod/robots.txt"
        ,
          src: "resources/.htaccess"
          dest: "target/prod/.htaccess"
        ,
          expand: true
          cwd: "resources/images"
          src: "**/*.*"
          dest: "target/prod/images"
        ]

    connect:
      proxies: [
        context: "/api"
        host: "localhost"
        port: 8080
        https: false
        changeOrigin: false
      ]
      dev:
        options:
          port: 9000
          hostname: "0.0.0.0"
          middleware: (connect) ->
            [require("grunt-connect-proxy/lib/utils").proxyRequest
            , mountFolder(connect, "target/dev/")
            , mountFolder(connect, "")
            , (req, res, next) ->
              req.url = "/"
              next()
            , (req, res, next) ->
              error = (err) ->
                res.statusCode = err.status or 500
                res.end err.message

              notFound = ->
                res.statusCode = 404
                res.end "Not found"

              if req.originalUrl.match(/\.(html|css|js|png|jpg|gif|ttf|woff|svg|eot)$/)
                notFound()
              else
                send(req, "_index.html").root("target/dev/").on("error", error).pipe res
            ]

    open:
      dev:
        url: "http://localhost:<%= connect.dev.options.port %>"

    watch:
      options:
        livereload: true

      coffeedev:
        files: ["src/**/*.coffee"]
        tasks: ["coffee:dev"]

      jade:
        files: ["resources/**/*.jade"]
        tasks: ["jade:dev"]

      less:
        files: ["resources/styles/**/*.less"]
        tasks: ["less:dev"]

      images:
        files: ["resources/images/**/*.*"]
        tasks: ["copy:dev"]

    clean:
      dev: ["target/dev/"]
      prod: ["target/prod/"]

  require("matchdep").filterDev("grunt-*").forEach grunt.loadNpmTasks
  grunt.registerTask "dev-build", ["clean:dev", "coffee:dev", "jade:dev", "less:dev", "copy:dev"]
  grunt.registerTask "prod-build", ["dev-build", "clean:prod", "copy:prod", "requirejs:prod", "jade:prod", "less:prod"]
  grunt.registerTask "dev", ["dev-build", "configureProxies", "open:dev", "connect:dev", "watch"]
  grunt.registerTask "prod", ["prod-build"]
  grunt.registerTask "default", ["dev"]
