using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using DatingApp.API.Helpers;
using DatingApp.API.Models;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDatingRepository _datingRepository;
        private readonly IMapper _mapper;

        public UsersController(IDatingRepository datingRepository, IMapper mapper)
        {
            _datingRepository = datingRepository;
            _mapper = mapper;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery]UserParams userParams)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var userFromRepo = await _datingRepository.GetUser(currentUserId, true);

            userParams.UserId = currentUserId;

            if (string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = userFromRepo.Gender == "male" ? "female" : "male";
            }
            var users = await _datingRepository.GetUsers(userParams);

            var usersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);

            Response.AddPagination(users.CurrentPage, users.PageSize,
            
                users.TotalCount, users.TotalPages);
           return Ok(usersToReturn);
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> Getuser(int id)
        {
           var isCurrentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) == id; 
           var user = await _datingRepository.GetUser(id, isCurrentUser);

           var userToReturn = _mapper.Map<UserForDetailedDto>(user);
           
           return Ok(userToReturn); 
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UserUpdate(int id, UserForUpdateDto userForUpdateDto)
        {
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
                var userFromRepo = await _datingRepository.GetUser(id, true);
                _mapper.Map(userForUpdateDto, userFromRepo);
                if(await _datingRepository.SaveAll())
                    return NoContent();
               // throw new Exception($"Updating user {id} Failed on save");  
               throw new Exception($"Updating user {id} Failed on save");
        }

        [HttpPost("{id}/like/{recipientId}")]
        public async Task<IActionResult> LikeUser(int id, int recipientId)
        {
            if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            var like = await _datingRepository.GetLike(id, recipientId);

            if (like != null)
                return BadRequest("You already like this user");

            if (await _datingRepository.GetUser(recipientId, false) == null)
                return NotFound();
            
            like = new Like 
            {
                LikerId = id,
                LikeeId = recipientId
            };
            _datingRepository.Add<Like>(like);

            if (await _datingRepository.SaveAll())
                return Ok();

             return BadRequest("Failed to like user");   
        }
    }
}