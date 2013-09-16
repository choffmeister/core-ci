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
define [], () ->
  class EventService
    @$inject = ["$http"]

    constructor: (@http) ->
      @listeners = {}
      @nextListenerId = 1
      @pushClientId = null

      @registerDomEvents()
      @registerPushEvents()

    listen: (namespace, name, args...) =>
      fqen = "#{namespace}:#{name}";

      # make sure that the listeners list is not null
      if not this.listeners[fqen]
        @listeners[fqen] = []

      # get new unique listener id
      listenerId = @nextListenerId++

      callback = null
      scope = null

      # search arguments for callback and scope
      for arg in args
        if not callback? and typeof arg == "function" then callback = arg
        if not scope? and typeof arg == "object" and arg?.$on? then scope = arg

      # if there is a callback then add it to listeners
      if callback?
        @listeners[fqen].push({ id: listenerId, callback: callback })
      # if there is a scope then register unlistening on scope destruction
      if scope?
        scope.$on "$destroy", () => @unlisten(listenerId)

      # return listener id
      listenerId

    unlisten: (listenerId) =>
      # search all namespaces and all listeners for id and remove it if found
      for fqen, listeners of @listeners
        for listener, i in listeners
          if listener.id == listenerId
            listeners.splice i, 1
            return true
      false

    emit: (namespace, name, data) =>
      fqen = "#{namespace}:#{name}"
      fqenMulti = "#{namespace}:*"

      # TODO: remove
      console.log fqen, data if name.substr(0,4) != 'tick'

      listeners = @listeners[fqen] ? []
      for listener in listeners
        listener.callback(data)
      listeners = @listeners[fqenMulti] ? []
      for listener in listeners
        listener.callback(data)

    registerDomEvents: () =>
      $(window).resize((event) => @emit("global", "resize", event))
      $(window).keydown((event) => @emit("global", "keydown", event))
      $(window).keypress((event) => @emit("global", "keypress", event))
      $(window).keyup((event) => @emit("global", "keyup", event))

      @globalTick(100)
      @globalTick(1000)
      @globalTick(5000)
      @globalTick(10000)

    registerPushEvents: () =>
      if @http?
        @listenForPushEvents()

    globalTick: (frequence) =>
      @emit("global", "tick#{frequence}", null);
      window.setTimeout (() =>
        @globalTick(frequence)), frequence

    listenForPushEvents: () =>
      @http({ method: "POST", url: "/api/push", data: { clientId: this._pushClientId } })
        .success (response, status, headers, config) =>
          @pushClientId = response.clientId;
          for message in response.messages
            @emit("push", message.key, message.value)

          window.setTimeout (() =>
            @listenForPushEvents()), 1
        .error (response, status, headers, config) =>
          window.setTimeout (() =>
            @listenForPushEvents()), 5000
