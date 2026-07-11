import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import { FEATURES, processTemplate, repositoryRoot } from '../build-web.mjs';
import path from 'node:path';

test('processTemplate resolves nested positive and negative feature blocks', () => {
  const source = [
    'before',
    '<!-- ###BEGIN###{Desktop} -->',
    'desktop',
    '// ###BEGIN###{!Mode-NodeWebkit}',
    'browser',
    '// ###END###{!Mode-NodeWebkit}',
    '<!-- ###END###{Desktop} -->',
    '<!-- ###BEGIN###{Mode-NodeWebkit} -->',
    'node-only',
    '<!-- ###END###{Mode-NodeWebkit} -->',
    'after',
  ].join('\n');

  assert.equal(processTemplate(source, FEATURES), ['before', 'desktop', 'browser', 'after'].join('\n'));
});

test('browser build removes NodeWebkit code and emits the feature manifest', async () => {
  const source = await readFile(path.join(repositoryRoot, 'index.html'), 'utf8');
  const output = processTemplate(source);

  assert.doesNotMatch(output, /###(?:BEGIN|END)###/);
  assert.doesNotMatch(output, /require\(['"]nw\.gui['"]\)/);
  assert.match(output, /"Mode-WebSite"/);
  assert.match(output, /styles-enhanced\.css/);
  assert.match(output, /MeshCommander Enhanced/);
});

test('processTemplate rejects unbalanced feature markers', () => {
  assert.throws(
    () => processTemplate('<!-- ###BEGIN###{Desktop} -->\ncontent\n<!-- ###END###{Terminal} -->'),
    /Unbalanced feature marker/,
  );
});
