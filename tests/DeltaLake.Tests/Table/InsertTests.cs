using DeltaLake.Errors;
using DeltaLake.Table;

namespace DeltaLake.Tests.Table;
public class InsertTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task Memory_Insert_Variable_Record_Count_Test(int length)
    {
        await BaseInsertTest($"memory://{Guid.NewGuid():N}", length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task File_System_Insert_Variable_Record_Count_Test(int length)
    {
        var info = DirectoryHelpers.CreateTempSubdirectory();
        try
        {
            await BaseInsertTest($"file://{info.FullName}", length);
        }
        finally
        {
            info.Delete(true);
        }
    }

    [Fact]
    public async Task Memory_Insert_Zero_Record_Count_Test()
    {
        var tableParts = await TableHelpers.SetupTable($"memory://{Guid.NewGuid():N}", 0);
        using var table = tableParts.table;
        var version = table.Version();
        await table.InsertAsync([], table.Schema(), new InsertOptions(), CancellationToken.None);
        Assert.Equal(version, table.Version());
    }

    [Fact]
    public async Task Insert_Stream_Test()
    {
        var tableParts = await TableHelpers.SetupTable($"memory://{Guid.NewGuid():N}", 0);
        using var table = tableParts.table;
        var version = table.Version();
        var rb = new[] {
            TableHelpers.BuildBasicRecordBatch(10),
        };
        var schema = table.Schema();
        using var reader = new Bridge.RecordBatchReader(rb, schema);
        await table.InsertAsync(reader, new InsertOptions(), CancellationToken.None);
        Assert.Equal(version + 1, table.Version());
    }

    [Fact]
    public async Task Memory_Insert_Will_Cancel_Test()
    {
        var tableParts = await TableHelpers.SetupTable($"memory://{Guid.NewGuid():N}", 0);
        using var table = tableParts.table;
        var version = table.Version();
        try
        {
            await table.InsertAsync([TableHelpers.BuildBasicRecordBatch(10)], table.Schema(), new InsertOptions(), new CancellationToken(true));
            throw new InvalidOperationException();
        }
        catch (OperationCanceledException)
        {
            Assert.Equal(version, table.Version());
        }
    }

    [Fact]
    public async Task Memory_Insert_Invalid_Schema_Options_Test()
    {
        await Assert.ThrowsAsync<DeltaConfigurationException>(async () =>
        {
            var options = new InsertOptions
            {
                SaveMode = SaveMode.Append,
                OverwriteSchema = true,
            };
            var tableParts = await TableHelpers.SetupTable($"memory://{Guid.NewGuid():N}", 1, options);
            using var table = tableParts.table;
            var version = table.Version();
            await table.InsertAsync([], table.Schema(), new InsertOptions(), CancellationToken.None);
            Assert.Equal(version, table.Version());
            throw new System.NotImplementedException();
        });
    }

    private async Task BaseInsertTest(string path, int length)
    {
        var data = await TableHelpers.SetupTable(path, length);
        using var table = data.table;
        var queryResult = table.QueryAsync(new SelectQuery("SELECT test FROM test WHERE test > 1")
        {
            TableAlias = "test",
        },
        CancellationToken.None).ToBlockingEnumerable().ToList();

        if (length > 2)
        {
            var totalRecords = queryResult.Select(s => s.Length).Sum();
            Assert.Equal(length - 2, totalRecords);
        }
        else
        {
            Assert.Empty(queryResult);
        }
    }

    private async Task StreamInsertTest(string path, int length)
    {
        var data = await TableHelpers.SetupTable(path, length);
        using var table = data.table;
        var queryResult = table.QueryAsync(new SelectQuery("SELECT test FROM test WHERE test > 1")
        {
            TableAlias = "test",
        },
        CancellationToken.None).ToBlockingEnumerable().ToList();

        if (length > 2)
        {
            var totalRecords = queryResult.Select(s => s.Length).Sum();
            Assert.Equal(length - 2, totalRecords);
        }
        else
        {
            Assert.Empty(queryResult);
        }
    }
}