using ASI.Basecode.Data;
using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.Manager;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly DbSet<User> _users;

        public UserService(IUserRepository repository, IUnitOfWork unitOfWork, AsiBasecodeDBContext dbContext, IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _users = dbContext?.Users ?? throw new ArgumentNullException(nameof(dbContext), "DbContext is null.");
        }

        public LoginResult AuthenticateUser(string userEmail, string password, ref User user)
        {
            user = null; // Initialize to null
            var passwordKey = PasswordManager.EncryptPassword(password);
            user = _repository.GetUsers().FirstOrDefault(x => x.Email == userEmail && x.Password == passwordKey);

            return user != null ? LoginResult.Success : LoginResult.Failed;
        }

        public void AddUser(UserViewModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var user = new User();
            if (!_repository.UserExists(model.Email))
            {
                _mapper.Map(model, user);
                user.Password = PasswordManager.EncryptPassword(model.Password);
                user.CreatedTime = DateTime.Now;
                user.UpdatedTime = DateTime.Now;
                user.CreatedBy = Environment.UserName;
                user.UpdatedBy = Environment.UserName;

                _repository.AddUser(user);
                _unitOfWork.SaveChanges(); // Commit the new user to the database
            }
            else
            {
                throw new InvalidDataException(Resources.Messages.Errors.UserExists);
            }
        }

        public void UpdateUser(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            _users.Update(user);           // Track the user entity for updates
            _unitOfWork.SaveChanges();      // Save changes and handle concurrency
        }
    }
}
