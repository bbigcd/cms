﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Datory;
using Microsoft.Extensions.Caching.Distributed;
using SiteServer.Abstractions;
using SiteServer.CMS.Caching;

namespace SiteServer.CMS.Repositories
{
    public partial class AccessTokenRepository : IRepository
    {
        private readonly Repository<AccessToken> _repository;

        public AccessTokenRepository()
        {
            _repository = new Repository<AccessToken>(new Database(WebConfigUtils.DatabaseType, WebConfigUtils.ConnectionString), CacheManager.Cache);
        }

        public IDatabase Database => _repository.Database;

        public string TableName => _repository.TableName;

        public List<TableColumn> TableColumns => _repository.TableColumns;

        public async Task<int> InsertAsync(AccessToken accessToken)
        {
            var token = WebConfigUtils.EncryptStringBySecretKey(StringUtils.Guid());
            accessToken.Token = token;
            accessToken.AddDate = DateTime.Now;
            accessToken.UpdatedDate = DateTime.Now;

            return await _repository.InsertAsync(accessToken);
        }

        public async Task<bool> UpdateAsync(AccessToken accessToken)
        {
            var cacheKey = GetCacheKeyByToken(accessToken.Token);
            return await _repository.UpdateAsync(accessToken, Q.CachingRemove(cacheKey));
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var accessToken = await _repository.GetAsync(id);
            if (accessToken == null) return false;

            var cacheKey = GetCacheKeyByToken(accessToken.Token);
            return await _repository.DeleteAsync(Q
                .Where(nameof(AccessToken.Id), id)
                .CachingRemove(cacheKey)
            ) == 1;
        }

        public async Task<string> RegenerateAsync(AccessToken accessToken)
        {
            var cacheKey = GetCacheKeyByToken(accessToken.Token);

            accessToken.Token = WebConfigUtils.EncryptStringBySecretKey(StringUtils.Guid());

            await _repository.UpdateAsync(accessToken, Q.CachingRemove(cacheKey));

            return accessToken.Token;
        }

        public async Task<bool> IsTitleExistsAsync(string title)
        {
            return await _repository.ExistsAsync(Q.Where(nameof(AccessToken.Title), title));
        }

        public async Task<IEnumerable<AccessToken>> GetAccessTokenListAsync()
        {
            return await _repository.GetAllAsync(Q.OrderBy(nameof(AccessToken.Id)));
        }

        public async Task<AccessToken> GetAsync(int id)
        {
            return await _repository.GetAsync(id);
        }

        public async Task<bool> IsScopeAsync(string token, string scope)
        {
            if (string.IsNullOrEmpty(token)) return false;

            var tokenInfo = await GetByTokenAsync(token);
            return tokenInfo != null && StringUtils.ContainsIgnoreCase(StringUtils.GetStringList(tokenInfo.Scopes), scope);
        }
    }
}
