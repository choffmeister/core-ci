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
  class DashboardController extends BaseController
    @$name = "DashboardController"

    init: () =>
      @listen "push", "projects", @updateThrottled
      @updateThrottled()

    update: () =>
      @api.get("projects").then (res) =>
        @scope.projects = res.projects
