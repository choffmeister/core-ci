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
  class LoginController extends BaseController
    @$name = "LoginController"

    init: () =>
      @scope.message = null
      @scope.submit = @submit

    submit: () =>
      if @scope.userName? and @scope.password?
        data =
          userName: @scope.userName
          password: @scope.password
          rememberMe: false

        @api.post("auth", data).then (res) =>
          @events.emit "authentication", "login", {
            userName: res.userName
          }
          @location.path("/")
        , (error) =>
          if error.status is 401
            @scope.message =
              text: "Invalid user name or password."
              type: "warning"
          else
            @scope.message =
              text: "An unknown error occured."
              type: "error"
      else
        @scope.message =
          text: "Please enter your user name and your password."
          type: "warning"

      @scope.password = null
