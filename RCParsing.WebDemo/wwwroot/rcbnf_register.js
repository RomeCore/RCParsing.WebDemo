function register_lang(id) {

	const lang_monarch = {
		defaultToken: '',
		tokenPostfix: '.rcgrammar',

		keywords: [
			'$keyword', '$identifier', '$number', '$pattern', '$main',
			'$skip', '$list', '$until', '$newline', '$whitespaces'
		],

		operators: [
			'|', ':', ';', ',', "'", '"', '[', ']', '{', '}', '(', ')'
		],

		symbols: /[|:';,()\[\]{}]/,

		tokenizer: {
			root: [
				[/\$[a-zA-Z_][\w]*/, {
					cases: {
						'@keywords': { token: 'keyword.$0' },
						'@default': 'type'
					}
				}],

				[/[a-zA-Z_][\w]*/, {
					cases: {
						'choice|of|sep|by|before|greedy|double': { token: 'keyword.control' },
						'@default': 'identifier'
					}
				}],

				[/[ \t\r\n]+/, 'white'],

				[/@symbols/, {
					cases: {
						'@operators': 'delimiter',
						'@default': ''
					}
				}],

				[/'[^']*'/, 'string'],

				[/"([^"\\]|\\.)*$/, 'string.invalid'], 
				[/"/, 'string', '@string'],

				[/\/[^\/]+\//, 'regexp'],

				[/\d+\.\d+/, 'number.float'],
				[/\d+/, 'number'],
			],

			string: [
				[/[^\\"]+/, 'string'],
				[/"/, 'string', '@pop']
			],
		},
	};

	monaco.languages.register({ id: id });
	monaco.languages.setMonarchTokensProvider(id, lang_monarch);

}
window.register_lang = register_lang;