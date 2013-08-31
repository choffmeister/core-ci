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
    @$inject = ["$http", "$q"]

    constructor: (@http, @q, @events) ->
      @baseUri = "/api/"

    get: (uri) =>
      @request("GET", uri, null)
    post: (uri, data) =>
      @request("POST", uri, data)
    put: (uri, data) =>
      @request("PUT", uri, data)
    delete: (uri) =>
      @request("DELETE", uri, null)

    request: (method, uri, data) =>
      deferred = @q.defer();

      @http({ method: method, url: @baseUri + uri, data: data })
      .success (response, status, headers, config) =>
          deferred.resolve(response)
      .error (response, status, headers, config) =>
          # at least log error to console
          console.log response, status, headers, config

          switch status
            when 400 then console.log @errorResponseToMessage(response)
            when 401 then console.log "Authorization required"
            when 404 then console.log @errorResponseToMessage(response)
            when 503 then console.log "Service is temporarily unavailable"
            when 500 then console.log "Internal server error"
            else
              console.log "An unknwon error occured"

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
