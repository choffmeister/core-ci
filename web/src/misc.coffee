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
define ["angular"], (angular) ->
  # helper filters
  angular.module("helperfilters", [])
    .filter "shaShort", () ->
      (sha) -> sha.substr(0, 8)
    .filter "guidShort", () ->
      (guid) -> guid.substr(0, 8)
    .filter "loremipsumTiny", () ->
      (text) -> "#{text}: Lorem ipsum dolor sit amet, consetetur sadipscing elitr."
    .filter "loremipsumShort", () ->
      (text) -> "#{text}: Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua."
    .filter "loremipsum", () ->
      (text) -> "#{text}: Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet."

  # goto directive
  angular.module("gotodirective", [])
    .directive "goto", ["$location", (location) -> {
      replace: false
      link: (scope, element, attrs) ->
        element.css "cursor", "pointer"
        element.click () ->
          scope.$apply () -> location.path(attrs.goto)
    }]
