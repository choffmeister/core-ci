/*
 * Copyright (C) 2013 Christian Hoffmeister
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see {http://www.gnu.org/licenses/}.
 */
using System;
using System.Linq;
using ServiceStack.ServiceInterface;
using CoreCI.Models;
using CoreCI.Contracts;

namespace CoreCI.Server.Services
{
    public class RegisterService : Service
    {
        private readonly IUserRepository _userRepository;

        public RegisterService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public override void Dispose()
        {
            _userRepository.Dispose();
        }

        public RegisterResponse Post(RegisterRequest req)
        {
            // TODO: hash passwords
            UserEntity newUser = new UserEntity()
            {
                Id = Guid.NewGuid(),
                UserName = Normalize(req.UserName),
                Email = Normalize(req.Email),
                PasswordHash = req.Password,
                PasswordSalt = string.Empty,
                PasswordHashAlgorithm = "plain"
            };

            // TODO: catch dup key exception and return proper response
            _userRepository.Insert(newUser);

            return new RegisterResponse();
        }

        private static string Normalize(string str)
        {
            return str.ToLowerInvariant().Trim();
        }
    }
}
