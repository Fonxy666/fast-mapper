using FastMapper;

namespace FastMapperTests;

public class User
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class UserDto
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class FastMapperTests
{
    [Fact]
    public void Should_Map_User_To_UserDto()
    {
        var user = new User { Name = "Alice", Age = 25 };

        var dto = Mapper<User, UserDto>.Map(user);

        Assert.Equal("Alice", dto.Name);
        Assert.Equal(25, dto.Age);
    }

    [Fact]
    public void Should_Not_Map_User_Name_To_UserDto()
    {
        var user = new User { Name = "Alice", Age = 25 };
        var mapConfig = new MapperConfig<User, UserDto>().Ignore(u => u.Name);

        var ignoredDto = Mapper<User, UserDto>.Map(user, mapConfig);
        var dto = Mapper<User, UserDto>.Map(user);

        Assert.Equal("Alice", dto.Name);
        Assert.Equal(string.Empty, ignoredDto.Name);
    }
}