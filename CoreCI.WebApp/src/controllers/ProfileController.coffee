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
  class ProfileController extends BaseController
    @$name = "ProfileController"

    init: () =>
      @scope.removeProject = @removeProject

      @listen "push", "connectors", @updateThrottled
      @listen "push", "projects", @updateThrottled
      @updateThrottled()

    update: () =>
      if @params.userName?
        @api.get("profile/#{@params.userName}").then (res) =>
          @scope.user = res.user
      else
        @api.get("profile").then (res) =>
          @scope.user = res.user
          @scope.connectors = res.connectors
          @scope.projects = res.projects

    removeProject: (project) =>
      @api.delete("/connector/#{project.connectorId}/projects/remove/#{project.id}").then (res) =>
        @emitMessage "success", "You have removed your project."
