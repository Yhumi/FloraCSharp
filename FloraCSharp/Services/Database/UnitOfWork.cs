﻿using FloraCSharp.Services.Database.Repos;
using FloraCSharp.Services.Database.Repos.Impl;
using System;
using System.Threading.Tasks;

namespace FloraCSharp.Services.Database
{
    class UnitOfWork : IUnitOfWork
    {
        public FloraContext _context { get; }
        private readonly FloraDebugLogger logger = new FloraDebugLogger();

        private IUserRatingRepository _userRatings;
        public IUserRatingRepository UserRatings => _userRatings ?? (_userRatings = new UserRatingRepository(_context));

        private IReactionsRepository _reactions;
        public IReactionsRepository Reactions => _reactions ?? (_reactions = new ReactionsRepository(_context));

        public UnitOfWork(FloraContext context)
        {
            _context = context;
        }

        public int Complete() =>
            _context.SaveChanges();

        public Task<int> CompleteAsync() =>
            _context.SaveChangesAsync();

        private bool disposed = false;

        protected void Dispose(bool disposing)
        {
            if (!this.disposed)
                if (disposing)
                    _context.Dispose();
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
