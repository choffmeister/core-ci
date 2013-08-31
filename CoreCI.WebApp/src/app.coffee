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
requirejs.config
  baseUrl: "/src"
  paths:
    jquery: "../bower_components/jquery/jquery"
    bootstrap: "../bower_components/bootstrap/dist/js/bootstrap"
    angular: "../bower_components/angular/angular"
    coreci: "coreci"
    basecontroller: "./controllers/BaseController"

  shim:
    bootstrap:
      deps: ["jquery"]
    angular:
      exports: "angular"
      deps: ["jquery"]
    coreci:
      deps: [
        "jquery"
        "bootstrap"
        "angular"
      ]

requirejs ["coreci"], (coreci) -> coreci.bootstrap()
