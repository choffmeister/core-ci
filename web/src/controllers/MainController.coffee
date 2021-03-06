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
define ["basecontroller"], (BaseController) ->
  class MainController extends BaseController
    @$name = "MainController"

    init: () =>
      @scope.messages = []
      @scope.isAuthenticated = false
      @scope.user = null

      @scope.dismissMessage = @dismissMessage

      @listen "authentication", "login", @onLogin
      @listen "authentication", "logout", @onLogout
      @listen "message", "*", @onMessage

      @checkLoginState()

    onLogin: (data) =>
      @scope.isAuthenticated = true
      @scope.user =
        userName: data.userName

    onLogout: () =>
      @scope.isAuthenticated = false
      @scope.user = null

    onMessage: (data) =>
      @scope.messages.push(data)

    dismissMessage: (messageToDismiss) =>
      for message, i in @scope.messages
        if message == messageToDismiss
          @scope.messages.splice i, 1

    checkLoginState: () =>
      @api.get("profile", false).then (res) =>
        @events.emit "authentication", "login", {
          userName: res.user.userName
        }
      , (error) =>
        @events.emit "authentication", "logout", null
