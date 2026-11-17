using System.Text.Json;
using PhoneBox.Agent;
using Xunit;

public class NoteEditTests
{
    private static (ConfigStore Store, string Path) Make()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"phonebox-note-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{"pages":[{"id":"p","title":"P","widgets":[{"id":"n","type":"note","options":{"text":"old"}},{"id":"c","type":"clock"}]}]}""");
        return (new ConfigStore(path), path);
    }

    [Fact]
    public void SetsTheTextOfANote()
    {
        var (cfg, path) = Make();
        try
        {
            var r = NoteEdit.Apply(cfg, "p", "n", "hello");
            Assert.Equal(200, r.Status);
            var w = cfg.Get().Pages!.First(p => p.Id == "p").Widgets!.First(w => w.Id == "n");
            Assert.Equal("hello", w.Options!["text"].GetString());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void RefusesMissingOrEmptyText()
    {
        var (cfg, path) = Make();
        try
        {
            Assert.Equal(400, NoteEdit.Apply(cfg, "p", "n", null).Status);
            Assert.Equal(400, NoteEdit.Apply(cfg, "p", "n", "").Status);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void RefusesTextOverTheLimit()
    {
        var (cfg, path) = Make();
        try { Assert.Equal(400, NoteEdit.Apply(cfg, "p", "n", new string('x', NoteEdit.MaxLength + 1)).Status); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void UnknownWidgetIsNotFound()
    {
        var (cfg, path) = Make();
        try { Assert.Equal(404, NoteEdit.Apply(cfg, "p", "missing", "hello").Status); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void OtherWidgetTypesAreRefused()
    {
        var (cfg, path) = Make();
        try { Assert.Equal(400, NoteEdit.Apply(cfg, "p", "c", "hello").Status); }
        finally { File.Delete(path); }
    }

    [Fact]
    public void AStaleVersionIsAConflict()
    {
        var (cfg, path) = Make();
        try { Assert.Equal(409, NoteEdit.Apply(cfg, "p", "n", "hello", expectedVersion: -1).Status); }
        finally { File.Delete(path); }
    }
}
