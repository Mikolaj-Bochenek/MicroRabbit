using System.Collections.Generic;
using MicroRabbit.Banking.Domain.Interfaces;
using MicroRabbit.Banking.Domain.Models;
using MicroRabbit.Banking.Infrastructure.Context;

namespace MicroRabbit.Banking.Infrastructure.Repository
{
    public class AccountRepository : IAccountRepository
    {
        private BankingDbContext _ctx;

        public AccountRepository(BankingDbContext ctx) => _ctx = ctx;
        
        public IEnumerable<Account> GetAccounts() => _ctx.Accounts;
    }
}