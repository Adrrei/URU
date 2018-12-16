using Microsoft.Extensions.Localization;
using System;

namespace URU.Models
{
    public class Repository : IRepository
    {
        private readonly AppDbContext _appDbContext;
        private readonly IStringLocalizer _stringLocalizer;

        public Repository(AppDbContext appDbContext, IStringLocalizer<Repository> stringLocalizer)
        {
            _appDbContext = appDbContext;
            _stringLocalizer = stringLocalizer;
        }

        public bool AddContact(Contact contact)
        {
            try
            {
                _appDbContext.Contacts.Add(contact);
                _appDbContext.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}