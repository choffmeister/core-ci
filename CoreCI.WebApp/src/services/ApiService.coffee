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
  class ApiService
    @$inject = ["$http", "$q", "events"]

    constructor: (@http, @q, @events) ->
      @baseUri = "/api/"

    get: (uri, emitErrors) =>
      @request("GET", uri, null, emitErrors)
    post: (uri, data, emitErrors) =>
      @request("POST", uri, data, emitErrors)
    put: (uri, data, emitErrors) =>
      @request("PUT", uri, data, emitErrors)
    delete: (uri, emitErrors) =>
      @request("DELETE", uri, null, emitErrors)

    request: (method, uri, data, emitErrors) =>
      deferred = @q.defer();

      @http({ method: method, url: @baseUri + uri, data: data })
        .success (response, status, headers, config) =>
          deferred.resolve(response)
        .error (response, status, headers, config) =>
          message = switch status
            when 400 then "Bad request"
            when 401 then "Authorization required"
            when 404 then "Not found"
            when 500 then "Internal server error"
            when 503 then "Service is temporarily unavailable"
            else "An unknwon error occured"
          type = switch status
            when 400 then "error"
            when 401 then "warning"
            when 404 then "warning"
            when 500 then "error"
            when 503 then "error"
            else "error"

          # at least log error to console
          console.log message, response, status, headers, config

          # emit an error message, if told so
          if not emitErrors? or emitErrors == true
            data =
              text: message
              type: type
            @events.emit "message", type, data

          deferred.reject {
            response: response
            status: status
            headers: headers
            config: config
          }

      deferred.promise

    errorResponseToMessage: (response) ->
      if response?.responseStatus?.message?
        response.responseStatus.message
      else
        response
