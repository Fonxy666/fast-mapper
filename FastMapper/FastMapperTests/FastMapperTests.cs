using FastMapper;
using Xunit.Abstractions;

namespace FastMapperTests;

public class User
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; } = 0;
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
    public int HouseNumber { get; set; }
    public Streets Streets { get; set; }
}

public class Streets
{
    public string street1 { get; set; } = string.Empty;
    public string street2 { get; set; } = string.Empty;
}

public class UserDto
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public Address Address { get; set; }
}

public class FastMapperTests
{
    private readonly ITestOutputHelper _output;

    public FastMapperTests(ITestOutputHelper output)
    {
        _output = output;
        MapperLogger.Log = msg => _output.WriteLine(msg);
    }

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
        var streets = new Streets { street1 = "aha", street2 = "hehe" };
        var address = new Address { City = "KirchBichl", HouseNumber = 5, Streets = streets };
        var user = new User { Name = "Alice", Age = 25, Address = address };
        var mapConfig = new MapperConfig<User, UserDto>().Ignore(u => u.Address.Streets.street1).Ignore(u => u.Address.City);

        var ignoredDto = Mapper<User, UserDto>.Map(user, mapConfig);
        var dto = Mapper<User, UserDto>.Map(user);
        Assert.Equal(string.Empty, ignoredDto.Address.City);
        Assert.Equal(string.Empty, ignoredDto.Address.Streets.street1);
    }
}