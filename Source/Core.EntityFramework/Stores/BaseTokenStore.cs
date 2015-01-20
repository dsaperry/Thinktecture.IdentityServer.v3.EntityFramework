﻿using Newtonsoft.Json;
/*
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
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Thinktecture.IdentityServer.Core.EntityFramework.Entities;
using Thinktecture.IdentityServer.Core.EntityFramework.Serialization;
using Thinktecture.IdentityServer.Core.Models;
using Thinktecture.IdentityServer.Core.Services;

namespace Thinktecture.IdentityServer.Core.EntityFramework
{
    public abstract class BaseTokenStore<T> where T : class
    {
        protected readonly OperationalDbContext context;
        protected readonly TokenType tokenType;
        protected readonly IScopeStore scopeStore;
        protected readonly IClientStore clientStore;

        protected BaseTokenStore(OperationalDbContext context, TokenType tokenType, IScopeStore scopeStore, IClientStore clientStore)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (scopeStore == null) throw new ArgumentNullException("scopeStore");
            if (clientStore == null) throw new ArgumentNullException("clientStore");
            
            this.context = context;
            this.tokenType = tokenType;
            this.scopeStore = scopeStore;
            this.clientStore = clientStore;
        }

        JsonSerializerSettings GetJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ClaimConverter());
            settings.Converters.Add(new ClaimsPrincipalConverter());
            settings.Converters.Add(new ClientConverter(clientStore));
            settings.Converters.Add(new ScopeConverter(scopeStore));
            return settings;
        }

        protected string ConvertToJson(T value)
        {
            return JsonConvert.SerializeObject(value, GetJsonSerializerSettings());
        }

        protected T ConvertFromJson(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, GetJsonSerializerSettings());
        }

        public async Task<T> GetAsync(string key)
        {
            var token = await context.Tokens.FirstOrDefaultAsync(c => c.Key == key && c.TokenType == tokenType).ConfigureAwait(false);
            
            if (token == null || token.Expiry < DateTimeOffset.UtcNow) return null;

            T value = ConvertFromJson(token.JsonCode);
            
            return value;
        }

        public async Task RemoveAsync(string key)
        {
            var code = await context.Tokens.FirstOrDefaultAsync(c => c.Key == key && c.TokenType == tokenType).ConfigureAwait(false);

            if (code != null)
            {
                context.Tokens.Remove(code);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ITokenMetadata>> GetAllAsync(string subject)
        {
            var tokens = await context.Tokens.Where(x => 
                x.SubjectId == subject &&
                x.TokenType == tokenType).ToArrayAsync().ConfigureAwait(false);
            
            var results = tokens.Select(x=>ConvertFromJson(x.JsonCode)).ToArray();
            
            return results.Cast<ITokenMetadata>();
        }
        
        public async Task RevokeAsync(string subject, string client)
        {
            var found = await context.Tokens.Where(x => 
                x.SubjectId == subject && 
                x.ClientId == client && 
                x.TokenType == tokenType).ToArrayAsync().ConfigureAwait(false);
            
            context.Tokens.RemoveRange(found);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        public abstract Task StoreAsync(string key, T value);
    }
}
