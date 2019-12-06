﻿using Aguacongas.IdentityServer.Store;
using Aguacongas.IdentityServer.Store.Entity;
using Community.OData.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aguacongas.IdentityServer.EntityFramework.Store
{
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "No localization")]
    public class AdminStore<T> : IAdminStore<T> where T: class, IEntityId, new()
    {
        private readonly IdentityServerDbContext _context;
        private readonly ILogger<AdminStore<T>> _logger;

        public AdminStore(IdentityServerDbContext context, ILogger<AdminStore<T>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<T> GetAsync(string id, GetRequest request, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<T>() as IQueryable<T>;
            query = query.Expand(request?.Expand);
            return query.Where(e => e.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<PageResponse<T>> GetAsync(PageRequest request, CancellationToken cancellationToken = default)
        {
            request = request ?? throw new ArgumentNullException(nameof(request));
            var query = _context.Set<T>() as IQueryable<T>;
            query = query.Expand(request.Expand);

            var odataQuery = query.OData();

            if (!string.IsNullOrEmpty(request.Filter))
            {
                odataQuery = odataQuery.Filter(request.Filter);
            }
            if (!string.IsNullOrEmpty(request.OrderBy))
            {
                odataQuery = odataQuery.OrderBy(request.OrderBy);
            }

            var count = await odataQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var page = odataQuery.Skip(request.Skip ?? 0);
            if (request.Take.HasValue)
            {
                page = page.Take(request.Take.Value);
            }

            var items = (await page.ToListAsync(cancellationToken).ConfigureAwait(false)) as IEnumerable<T>;

            return new PageResponse<T>
            {
                Count = count,
                Items = items
            };
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Set<T>().FindAsync(id, cancellationToken).ConfigureAwait(false);
            if (entity == null)
            {
                throw new DbUpdateException($"Entity type {typeof(T).Name} at id {id} is not found");
            }
            _context.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Entity {EntityId} deleted", entity.Id, entity);
        }

        public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            entity = entity ?? throw new ArgumentNullException(nameof(entity));
            if (entity.Id == null)
            {
                entity.Id = Guid.NewGuid().ToString();
            }
            await _context.AddAsync(entity, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Entity {EntityId} created", entity.Id, entity);
            return entity;
        }

        public Task<IEntityId> CreateAsync(IEntityId entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            entity = entity ?? throw new ArgumentNullException(nameof(entity));
            var storedEntity = await _context.Set<T>().FindAsync(entity.Id, cancellationToken).ConfigureAwait(false);
            if (storedEntity == null)
            {
                throw new DbUpdateException($"Entity type {typeof(T).Name} at id {entity.Id} is not found");
            }
            if (entity is IAuditable auditable)
            {
                var storedAuditable = storedEntity as IAuditable;
                auditable.CreatedAt = storedAuditable.CreatedAt;
                auditable.ModifiedAt = storedAuditable.ModifiedAt;
            }
            _context.Update(entity);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Entity {EntityId} updated", entity.Id, entity);
            return entity;
        }

        public Task<IEntityId> UpdateAsync(IEntityId entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
