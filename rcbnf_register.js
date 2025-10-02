function register_lang(id) {

	const lang_monarch = {
		defaultToken: '',
		tokenPostfix: '.rcgrammar',

		keywords: [
			'$keyword', '$identifier', '$number', '$main', '$skip', '$tokenizer'
		],

		operators: [
			'|', ':', ';', '[', ']', '{', '}', '(', ')', '%'
		],

		symbols: /[|:;()\[\]{}]/,

		tokenizer: {
			root: [
				[/\$[a-zA-Z_][\w]*/, {
					cases: {
						'@keywords': { token: 'keyword.$0' },
						'@default': 'type'
					}
				}],

				[/[a-zA-Z][\w]*/, {
					cases: {
						'before|after|lazy|greedy': { token: 'keyword.control' },
						'EOF|WS|SPACES': { token: 'keyword.control' },
						'@default': 'identifier'
					}
				}],

				[/[ \t\r\n]+/, 'white'],

				[/'[^'\n\r]*'/, 'string'],

				[/\/(?:\\\/|[^\/])*\//, 'regexp'],

				[/@symbols/, {
					cases: {
						'@operators': 'delimiter',
						'@default': ''
					}
				}],

				[/\d+\.\d+/, 'number.float'],
				[/\d+/, 'number'],

				[/#[^\n\r]*/, 'comment']
			],
		},
	};

	monaco.languages.register({ id: id });
	monaco.languages.setMonarchTokensProvider(id, lang_monarch);
}
window.register_lang = register_lang;