using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;

    private readonly IMapper mapper;

    // Чтобы ASP.NET положил что-то в userRepository требуется конфигурация
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [Produces("application/json", "application/xml")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    [HttpHead("{userId}")]
    public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user == null)
            return NotFound();
        var userDto = mapper.Map<UserDto>(user);
        if (HttpMethods.IsHead(Request.Method))
        {
            Response.Headers.ContentType = "application/json; charset=utf-8";
            return Ok();
        }

        return Ok(userDto);
    }

    [HttpPost]
    [Produces("application/json", "application/xml")]
    public IActionResult CreateUser([FromBody] CreateUserDto createUser)
    {
        if (createUser == null)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = mapper.Map<UserEntity>(createUser);
        userEntity = userRepository.Insert(userEntity);
        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = userEntity.Id },
            userEntity.Id);
    }

    [HttpPut("{userId}")]
    [Produces("application/json", "application/xml")]
    public IActionResult UpdatetUser([FromRoute] Guid userId, [FromBody] UpdateUserDto? updateUser)
    {
        if (updateUser == null || userId == Guid.Empty)
            return BadRequest();
        if (!ModelState.IsValid)
            return UnprocessableEntity(ModelState);
        var userEntity = userRepository.FindById(userId) ?? new UserEntity(userId);
        userEntity = mapper.Map(updateUser, userEntity);
        userRepository.UpdateOrInsert(userEntity, out var isInserted);
        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = userEntity.Id },
                userEntity.Id);
        }

        return NoContent();
    }

    [Produces("application/json", "application/xml")]
    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto> patchDoc)
    {
        if (patchDoc == null)
            return BadRequest();
        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();
        var updateDto = mapper.Map<UpdateUserDto>(userEntity);
        patchDoc.ApplyTo(updateDto, ModelState);
        if (!TryValidateModel(updateDto))
            return UnprocessableEntity(ModelState);
        userEntity = mapper.Map(updateDto, userEntity);
        userRepository.Update(userEntity);
        return NoContent();
    }

    [Produces("application/json", "application/xml")]
    [HttpDelete("{userId}")]
    public IActionResult DeleteUser(Guid userId)
    {
        var userEntity = userRepository.FindById(userId);
        if (userEntity == null)
            return NotFound();
        userRepository.Delete(userId);
        return NoContent();
    }

    [Produces("application/json", "application/xml")]
    [HttpOptions]
    public IActionResult GetOptions()
    {
        Response.Headers.Add("Allow", "GET,POST,OPTIONS");
        return Ok();
    }
}