using System;
using System.IO;
using UOScript;
using Xunit;

namespace ClassicUO.UnitTests.LegionScripting
{
    public class UOScriptParseTest
    {
        [Fact]
        public void ParseCraftFillBodScript_NoSyntaxError()
        {
            string script = @"@removelist 'Amount'
@removelist 'MaterialGraphic'
@createlist 'Amount'
@createlist 'MaterialGraphic'
@setalias 'Restock' 0x45318201
@clearjournal
if not hidden 'self'
  useskill 'hiding'
endif
if not @findobject 'FilledBods' 'any' 'backpack' or not @findobject 'EmptyBods' 'backpack'
  clearignorelist
  if @findtype 0x2258 'any' 'backpack'
    sysmsg 'Get loose bods out of pack before starting' 34
    stop
  endif
endif
if @findtype 0x2258 'any' 'backpack' and @property 'small' 'found'
  @setalias 'bod' 'found'
  useobject! 'bod'
  waitforgump 0x5afbd742 15000
  if @ingump 0x5afbd742 'leather gorget'
    @pushlist 'GumpCat' 36
    @pushlist 'GumpSel' 23
  elseif @ingump 0x5afbd742 'leather cap'
    @pushlist 'GumpCat' 36
    @pushlist 'GumpSel' 30
  endif
  while targetexists 'server'
    if not @findalias 'crafting'
      if @ingump 0x5afbd742 'leather gorget'
        @pushlist 'Graphic' 0x13c7
      endif
    endif
  endwhile
elseif not @property 'Deeds in Book: 0' 'EmptyBods'
  useobject! 'EmptyBods'
  waitforgump 0x54f555df 2500
else
  sysmsg 'Out of bods to fill.' 64
  @playmacro 'CraftFill'
  stop
endif
";
            string[] lines = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            ASTNode ast = Lexer.Lex(lines);
            Assert.NotNull(ast);
            Script s = new Script(ast);
            Assert.NotNull(s);
        }

        [Fact]
        public void ParseScriptFromFile_MatchesLexLinesBehavior()
        {
            string script = @"@createlist 'TestList'
@pushlist 'TestList' 1
if not hidden 'self'
  useskill 'hiding'
endif
pause 100
";
            string tmpPath = Path.Combine(Path.GetTempPath(), "test_parse_" + Guid.NewGuid().ToString("N") + ".uos");
            try
            {
                File.WriteAllText(tmpPath, script);
                ASTNode ast = Lexer.Lex(tmpPath);
                Assert.NotNull(ast);
                Script s = new Script(ast);
                Assert.NotNull(s);
            }
            finally
            {
                if (File.Exists(tmpPath))
                    File.Delete(tmpPath);
            }
        }
    }
}
