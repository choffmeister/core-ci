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
define ["underscore", "angular"], (_, angular) ->
  class BaseController
    @$inject = ["$scope", "$routeParams", "$location", "api", "events"]

    constructor: (@scope, @params, @location, @api, @events) ->
      @listenerIds = []
      @updateThrottled = _.throttle(@update, 1000)
      @scope.params = @params
      @scope.$on "$destroy", () =>
        @unlisten()
        @dispose()
      @init()

    init: () =>
      # do nothing

    dispose: () =>
      # do nothing

    update: () =>
      # do nothing

    emitMessage: (type, text) =>
      @events.emit "message", type,
        type: type
        text: text

    listen: (namespace, name, callback) =>
      @listenerIds.push(@events.listen(namespace, name, callback))

    unlisten: () =>
      for listenerId in @listenerIds
        @events.unlisten(listenerId)
