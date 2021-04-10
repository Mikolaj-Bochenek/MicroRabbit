using System.Collections.Generic;
using MicroRabbit.Banking.Application.Interfaces;
using MicroRabbit.Banking.Domain.Interfaces;
using MicroRabbit.Banking.Domain.Models;

namespace MicroRabbit.Banking.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountrepository;

        public AccountService(IAccountRepository accountrepository) => _accountrepository = accountrepository;
        
        public IEnumerable<Account> GetAccounts() => _accountrepository.GetAccounts();
    }
}