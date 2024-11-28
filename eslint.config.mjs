import pluginJs from "@eslint/js";
import typescriptEslint from "@typescript-eslint/eslint-plugin";
import tsParser from "@typescript-eslint/parser";
import prettier from "eslint-config-prettier";
import jsdoc from "eslint-plugin-jsdoc";
import node from "eslint-plugin-n";
import globals from "globals";
import tseslint from "typescript-eslint";

export default [
  {
    ignores: ["**/coverage", "**/dist", "**/eslint.config.mjs", "src/NodeJS/Generated/*"],
  },
  pluginJs.configs.recommended,
  ...tseslint.configs.strict,
  ...tseslint.configs.strictTypeChecked,
  ...tseslint.configs.stylisticTypeChecked,
  node.configs["flat/recommended-script"],
  jsdoc.configs["flat/recommended-typescript"],
  prettier,
  {
    files: ["src/NodeJS/**/*.{ts,mts}"],

    plugins: {
      "@typescript-eslint": typescriptEslint,
      jsdoc,
    },

    languageOptions: {
      ecmaVersion: 2020,
      globals: {
        ...globals.node,
      },
      parser: tsParser,
      parserOptions: {
        project: ["./tsconfig.json"],
        tsConfigRootDir: import.meta.dirname,
      },
      sourceType: "module",
    },

    settings: {
      jsdoc: {
        mode: "typescript",
      },

      node: {
        tryExtensions: [".js", ".ts", ".mjs", ".mts"],
      },
    },

    rules: {
      "comma-dangle": ["error", "always-multiline"],
      "dot-notation": ["error"],

      "jsdoc/no-types": "off",

      // "n/no-unsupported-features/es-syntax": [
      //   "error",
      //   {
      //     version: ">=14.0.0",
      //     ignores: ["modules"],
      //   },
      // ],

      "@typescript-eslint/no-unused-vars": [
        "warn",
        {
          vars: "all",
          args: "all",
          argsIgnorePattern: "^_",
        },
      ],
    },
  },
];
