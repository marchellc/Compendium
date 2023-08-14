using System.Collections.Generic;

namespace Compendium.Commands.Parsing
{
    public static class StringParserSettings
    {
        public static IReadOnlyDictionary<char, char> AliasMap { get; } = new Dictionary<char, char>
        {
            {'\"', '\"' },
            {'«', '»' },
            {'‘', '’' },
            {'“', '”' },
            {'„', '‟' },
            {'‹', '›' },
            {'‚', '‛' },
            {'《', '》' },
            {'〈', '〉' },
            {'「', '」' },
            {'『', '』' },
            {'〝', '〞' },
            {'﹁', '﹂' },
            {'﹃', '﹄' },
            {'＂', '＂' },
            {'＇', '＇' },
            {'｢', '｣' },
            {'(', ')' },
            {'༺', '༻' },
            {'༼', '༽' },
            {'᚛', '᚜' },
            {'⁅', '⁆' },
            {'⌈', '⌉' },
            {'⌊', '⌋' },
            {'❨', '❩' },
            {'❪', '❫' },
            {'❬', '❭' },
            {'❮', '❯' },
            {'❰', '❱' },
            {'❲', '❳' },
            {'❴', '❵' },
            {'⟅', '⟆' },
            {'⟦', '⟧' },
            {'⟨', '⟩' },
            {'⟪', '⟫' },
            {'⟬', '⟭' },
            {'⟮', '⟯' },
            {'⦃', '⦄' },
            {'⦅', '⦆' },
            {'⦇', '⦈' },
            {'⦉', '⦊' },
            {'⦋', '⦌' },
            {'⦍', '⦎' },
            {'⦏', '⦐' },
            {'⦑', '⦒' },
            {'⦓', '⦔' },
            {'⦕', '⦖' },
            {'⦗', '⦘' },
            {'⧘', '⧙' },
            {'⧚', '⧛' },
            {'⧼', '⧽' },
            {'⸂', '⸃' },
            {'⸄', '⸅' },
            {'⸉', '⸊' },
            {'⸌', '⸍' },
            {'⸜', '⸝' },
            {'⸠', '⸡' },
            {'⸢', '⸣' },
            {'⸤', '⸥' },
            {'⸦', '⸧' },
            {'⸨', '⸩' },
            {'【', '】'},
            {'〔', '〕' },
            {'〖', '〗' },
            {'〘', '〙' },
            {'〚', '〛' }
        };

        public static bool IsOpenQuote(char quote)
            => AliasMap.ContainsKey(quote) || quote == '\"';

        public static char GetMatchingQuote(char quote)
            => AliasMap.TryGetValue(quote, out var matching) ? matching : '\"';
    }
}