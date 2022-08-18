using System.Reactive.Linq;
using DynamicData;
using ModernX2Launcher.Utilities;

namespace ModernX2Launcher.Tests.Utilities.Operators;

public class FlattenConcatChangeSetsTest
{
    [Fact]
    public void SingleInnerCollection()
    {
        SourceList<SourceList<string>> outerList = new();

        using IObservableList<string> resultList = outerList.Connect()
            .FlattenConcat<SourceList<string>, string>()
            .AsObservableList();

        Assert.Equal(0, resultList.Count);

        SourceList<string> innerList = new();

        outerList.Add(innerList);
        Assert.Equal(Array.Empty<string>(), resultList.Items);

        innerList.Add("a");
        Assert.Equal(new[] { "a" }, resultList.Items);

        innerList.Add("b");
        Assert.Equal(new[] { "a", "b" }, resultList.Items);

        innerList.Insert(0, "c");
        Assert.Equal(new[] { "c", "a", "b" }, resultList.Items);

        innerList.Move(0, 2);
        Assert.Equal(new[] { "a", "b", "c" }, resultList.Items);

        innerList.RemoveRange(0, 2);
        Assert.Equal(new[] { "c" }, resultList.Items);

        innerList.Edit(list =>
        {
            list.Add("d");
            list.Add("e");

            list[0] = "C";
        });
        Assert.Equal(new[] { "C", "d", "e" }, resultList.Items);
        
        outerList.Clear();
        Assert.Equal(Array.Empty<string>(), resultList.Items);

    }
}