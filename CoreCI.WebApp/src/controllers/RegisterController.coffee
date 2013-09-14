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
  class RegisterController extends BaseController
    @$name = "RegisterController"

    init: () =>
      @scope.message = null
      @scope.submit = @submit

    submit: () =>
      if @scope.password is @scope.passwordRepeated
        data =
          userName: @scope.userName
          email: @scope.email
          password: @scope.password

        @api.post("register", data, false).then (res) =>
          @location.path("/login")
        , (error) =>
          if error.status is 400
            @scope.message =
              text: "Registration has been rejected."
              type: "warning"
          else
            @scope.message =
              text: "An unknown error occured."
              type: "error"
      else
        @scope.message =
          text: "Please enter the password twice."
          type: "warning"
