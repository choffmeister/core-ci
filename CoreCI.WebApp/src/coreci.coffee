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
define [
  "angular"
  "./services/ApiService"
  "./services/EventService"
  "./controllers"
  "./routes"
  "./misc"
],
(angular, ApiService, EventService) ->
  bootstrap: () ->
    # services
    angular.module("coreci.services", [])
      .factory("api", ["$http", "$q", (http, q) => new ApiService(http, q)])
      .factory('events', ["$http", (http) -> new EventService(http)])

    # compose
    angular.module "coreci", [
      "coreci.services"
      "coreci.controllers"
      "coreci.routes"
      "helperfilters"
      "gotodirective"
    ]

    # bootstrap
    angular.element(document).ready () -> angular.bootstrap document, ["coreci"]
