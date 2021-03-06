﻿/*
 * Copyright 2014 Dominick Baier, Brock Allen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Threading.Tasks;
using Thinktecture.IdentityServer.Core.EntityFramework.Entities;
using Thinktecture.IdentityServer.Core.Models;
using Thinktecture.IdentityServer.Core.Services;

namespace Thinktecture.IdentityServer.Core.EntityFramework
{
    public class RefreshTokenStore : BaseTokenStore<RefreshToken>, IRefreshTokenStore
    {
        public RefreshTokenStore(string connectionstring, IScopeStore scopeStore, IClientStore clientStore)
            : base(connectionstring, TokenType.RefreshToken, scopeStore, clientStore)
        {
        }

        public override Task StoreAsync(string key, RefreshToken value)
        {
            using (var db = new OperationalDbContext(ConnectionString))
            {
                var efToken = new Entities.Token
                {
                    Key = key,
                    SubjectId = value.SubjectId,
                    ClientId = value.ClientId,
                    JsonCode = ConvertToJson(value),
                    Expiry = DateTimeOffset.UtcNow.AddSeconds(value.LifeTime),
                    TokenType = TokenType
                };

                db.Tokens.Add(efToken);
                db.SaveChanges();
            }

            return Task.FromResult(0);
        }
    }
}
