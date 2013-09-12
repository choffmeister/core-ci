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
  angular.module("coreci.routes", [])
    .config ["$routeProvider", "$locationProvider", (routeProvider, locationProvider) ->
      routeProvider
        .when("/", { redirectTo: "/dashboard" })
        .when("/login", { templateUrl: "/views/login.html", controller: "LoginController" })
        .when("/logout", { templateUrl: "/views/logout.html", controller: "LogoutController" })
        .when("/auth/:provider", { templateUrl: "/views/authentication.html", controller: "AuthenticationController" })
        .when("/dashboard", { templateUrl: "/views/dashboard.html", controller: "DashboardController" })
        .when("/project/:projectId", { templateUrl: "/views/project.html", controller: "ProjectController" })
        .when("/task/:taskId", { templateUrl: "/views/task.html", controller: "TaskController" })
        .otherwise({ templateUrl: "/views/notfound.html" })

      locationProvider.html5Mode(true)
    ]
