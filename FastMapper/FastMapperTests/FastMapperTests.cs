using FastMapper;
using FastMapper.Model;
using Xunit;
using Xunit.Abstractions;

namespace FastMapperTests;

public class FastMapperTests
{
    private readonly ITestOutputHelper _output;

    public FastMapperTests(ITestOutputHelper output)
    {
        _output = output;
        MapperLogger.Log = msg => _output.WriteLine(msg);
    }

    private class TestUser
    {
        public required string Name { get; set; } = "TestUser";
        public int Age { get; set; } = 0;
        public TestAddress? Address { get; set; }
    }

    private class TestAddress
    {
        public required string City { get; set; } = "TestCity";
        public required int HouseNumber { get; set; } = 1;
        public TestStreets? Streets { get; set; }
    }

    private class TestStreets
    {
        public string? Street1 { get; set; } = string.Empty;
        public string? Street2 { get; set; } = string.Empty;
    }

    private class TestUserDto
    {
        public required string Name { get; set; } = string.Empty;
        public required int Age { get; set; }
        public TestAddress? Address { get; set; }
    }

    private class TestPartialUserDto
    {
        public required string Name { get; set; } = string.Empty;
    }

    private static readonly TestStreets MockTestStreets =
        new()
        {
            Street1 = "test-street",
            Street2 = "test-street-2"
        };

    private static readonly TestAddress MockTestAddress =
        new()
        {
            City = "KirchBichl",
            HouseNumber = 5,
            Streets = MockTestStreets
        };

    private static readonly TestUser MockTestUser =
        new()
        {
            Name = "Alice",
            Age = 25,
            Address = MockTestAddress
        };

    private static TestStreets CloneStreets(TestStreets s) =>
        new()
        {
            Street1 = s.Street1,
            Street2 = s.Street2
        };

    private static TestAddress CloneAddress(TestAddress a) =>
        new()
        {
            City = a.City,
            HouseNumber = a.HouseNumber,
            Streets = a.Streets != null ? CloneStreets(a.Streets) : null
        };

    private static TestUser CloneUser(TestUser u) =>
        new()
        {
            Name = u.Name,
            Age = u.Age,
            Address = u.Address != null ? CloneAddress(u.Address) : null
        };

    [Fact]
    public void Should_Map_User_To_UserDto()
    {
        var user = CloneUser(MockTestUser);

        var dto = Mapper<TestUser, TestUserDto>.Map(user);

        Assert.Equal("Alice", dto.Name);
        Assert.Equal("test-street", dto.Address!.Streets!.Street1);
    }

    [Fact]
    public void Should_Not_Map_Ignored_Nested_Properties()
    {
        var user = CloneUser(MockTestUser);

        var config = new MapperConfig<TestUser, TestUserDto>()
            .Ignore(u => u.Address!.City)
            .Ignore(u => u.Address!.Streets!.Street1);

        var dto = Mapper<TestUser, TestUserDto>.Map(user, config);

        Assert.Null(dto.Address!.City);
        Assert.Null(dto.Address!.Streets!.Street1);
        Assert.Equal("test-street-2", dto.Address!.Streets!.Street2);
    }

    [Fact]
    public void Should_Ignore_Root_Property()
    {
        var user = CloneUser(MockTestUser);

        var config = new MapperConfig<TestUser, TestUserDto>()
            .Ignore(u => u.Name);

        var dto = Mapper<TestUser, TestUserDto>.Map(user, config);

        Assert.Null(dto.Name);
        Assert.Equal(25, dto.Age);
    }

    [Fact]
    public void Should_Map_Null_Nested_Object_To_Null()
    {
        var user = CloneUser(MockTestUser);
        user.Address = null;

        var dto = Mapper<TestUser, TestUserDto>.Map(user);

        Assert.Null(dto.Address);
    }

    [Fact]
    public void Should_Map_Null_Deep_Nested_Object_To_Null()
    {
        var user = CloneUser(MockTestUser);
        user.Address!.Streets = null;

        var dto = Mapper<TestUser, TestUserDto>.Map(user);

        Assert.NotNull(dto.Address);
        Assert.Null(dto.Address!.Streets);
    }

    [Fact]
    public void Should_Ignore_Deep_Nested_Property_Only()
    {
        var user = CloneUser(MockTestUser);

        var config = new MapperConfig<TestUser, TestUserDto>()
            .Ignore(u => u.Address!.Streets!.Street2);

        var dto = Mapper<TestUser, TestUserDto>.Map(user, config);

        Assert.Equal("test-street", dto.Address!.Streets!.Street1);
        Assert.Null(dto.Address!.Streets!.Street2);
    }

    [Fact]
    public void Default_Map_Should_Not_Be_Affected_By_Config_Map()
    {
        var user = CloneUser(MockTestUser);

        var config = new MapperConfig<TestUser, TestUserDto>()
            .Ignore(u => u.Name);

        var ignoredDto = Mapper<TestUser, TestUserDto>.Map(user, config);
        var normalDto = Mapper<TestUser, TestUserDto>.Map(user);

        Assert.Null(ignoredDto.Name);
        Assert.Equal("Alice", normalDto.Name);
    }

    [Fact]
    public void Missing_Source_Property_Should_Not_Throw()
    {
        var user = CloneUser(MockTestUser);

        var dto = Mapper<TestUser, TestPartialUserDto>.Map(user);

        Assert.Equal("Alice", dto.Name);
    }

    [Fact]
    public void Map_Should_Be_Idempotent()
    {
        var user = CloneUser(MockTestUser);

        var dto1 = Mapper<TestUser, TestUserDto>.Map(user);
        var dto2 = Mapper<TestUser, TestUserDto>.Map(user);

        Assert.Equal(dto1.Name, dto2.Name);
        Assert.Equal(dto1.Age, dto2.Age);
    }
}