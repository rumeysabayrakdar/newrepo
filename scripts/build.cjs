const {
  copyFileSync,
  mkdirSync,
  readFileSync,
  writeFileSync,
} = require('node:fs');
const { join, resolve } = require('node:path');
const babel = require('@babel/core');

const root = resolve(__dirname, '..');
const sourceDirectory = join(root, 'frontend');
const outputDirectory = join(root, 'dist');

mkdirSync(outputDirectory, { recursive: true });
const { code } = babel.transformFileSync(join(sourceDirectory, 'app.jsx'), {
  presets: ['@babel/preset-react'],
  comments: false,
});
writeFileSync(join(outputDirectory, 'app.js'), code + '\n');

for (const file of [
  'experience-template.js',
  'experience.css',
  'review-fixes.css',
  'fonts.css',
]) {
  copyFileSync(join(sourceDirectory, file), join(outputDirectory, file));
}

// The catalogue runs as a classic script in both the deployed app and offline preview.
const renderer = ['interface-text.js', 'experience-view.js']
  .map((file) => readFileSync(join(sourceDirectory, file), 'utf8'))
  .join('\n');
writeFileSync(
  join(outputDirectory, 'experience-view.js'),
  `(() => {
${renderer}
})();
`,
);

console.log('Built frontend assets in dist/');
