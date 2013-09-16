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
        .when("/register", { templateUrl: "/views/register.html", controller: "RegisterController" })
        .when("/connect/:connectorName", { templateUrl: "/views/connect.html", controller: "ConnectController" })
        .when("/dashboard", { templateUrl: "/views/dashboard.html", controller: "DashboardController" })
        .when("/profile", { templateUrl: "/views/profile-private.html", controller: "ProfileController" })
        .when("/profile/:userName", { templateUrl: "/views/profile-public.html", controller: "ProfileController" })
        .when("/project/:projectId", { templateUrl: "/views/project.html", controller: "ProjectController" })
        .when("/project/add/:connectorName/:connectorId", { templateUrl: "/views/project-add.html", controller: "ProjectAddController" })
        .when("/task/:taskId", { templateUrl: "/views/task.html", controller: "TaskController" })
        .otherwise({ templateUrl: "/views/notfound.html" })

      locationProvider.html5Mode(true)
    ]
