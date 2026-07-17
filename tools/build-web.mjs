import { cp, mkdir, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const toolsDirectory = path.dirname(fileURLToPath(import.meta.url));
export const repositoryRoot = path.resolve(toolsDirectory, '..');

export const FEATURES = new Set([
  'AgentPresence',
  'Alarms',
  'AuditLog',
  'CertificateManager',
  'Certificates',
  'ComputerSelector',
  'ComputerSelector-Local',
  'ComputerSelectorToolbar',
  'ContextMenus',
  'Desktop',
  'DesktopFocus',
  'Desktop-Multi',
  'DesktopRotation',
  'Desktop-Settings',
  'DesktopType',
  'EventLog',
  'EventSubscriptions',
  'FileSaver',
  'HardwareInfo',
  'IDER',
  'IDERStats',
  'Inflate',
  'Look-Commander',
  'Mode-WebSite',
  'NetworkSettings',
  'PowerControl',
  'PowerControl-Advanced',
  'RemoteAccess',
  'Scripting',
  'Scripting-Editor',
  'SessionRecording',
  'Storage',
  'SystemDefense',
  'Terminal',
  'Terminal-Enumation-All',
  'Terminal-FxEnumation-All',
  'TerminalReplay',
  'TerminalSize',
  'USBSetup',
  'VersionWarning',
  'Wireless',
  'WsmanBrowser',
]);

const copiedDirectories = [
  'forge.js',
  'images',
  'images-commander',
  'pki.js',
  'trustedCA',
];

const copiedRootExtensions = new Set(['.css', '.gif', '.ico', '.js', '.png', '.svg']);

function featureEnabled(expression, features) {
  if (expression.startsWith('**')) return false;
  if (expression.startsWith('!')) return !features.has(expression.slice(1));
  return features.has(expression);
}

export function processTemplate(source, features = FEATURES, options = {}) {
  const output = [];
  const stack = [];
  const isActive = () => stack.every((expression) => featureEnabled(expression, features));

  for (const line of source.split(/\r?\n/)) {
    const marker = line.match(/###(BEGIN|END)###\{([^}]+)\}/);
    if (marker) {
      const [, operation, expression] = marker;
      if (operation === 'BEGIN') {
        stack.push(expression);
      } else {
        const index = stack.lastIndexOf(expression);
        if (index === -1) {
          throw new Error(`Unbalanced feature marker: ${operation} ${expression}`);
        }
        stack.splice(index, 1);
      }
      continue;
    }

    if (isActive()) output.push(line);
  }

  if (stack.length !== 0 && !options.tolerateUnclosed) {
    throw new Error(`Unclosed feature marker: ${stack.at(-1)}`);
  }

  const featureLiteral = [...features]
    .sort((left, right) => left.localeCompare(right))
    .map((feature) => JSON.stringify(feature))
    .join(',');

  return output
    .join('\n')
    .replace('/*###WEBCOMPILERFEATURES###*/', featureLiteral)
    .replace(/<title>MeshCommander<\/title>/g, '<title>MeshCommander Enhanced</title>')
    .replace(/<p class="top1">MeshCommander<\/p>/g, '<p class="top1">MeshCommander <span class="enhanced-mark">Enhanced</span></p>')
    .replace(
      '</head>',
      '    <meta name="viewport" content="width=device-width, initial-scale=1" />\n' +
      '    <meta name="theme-color" content="#0b1220" />\n' +
        '    <script src="desktop-bootstrap.js"></script>\n' +
        '    <link rel="stylesheet" href="styles-enhanced.css" />\n' +
        '</head>',
    );
}

async function copyRootAssets(outputDirectory) {
  const entries = await readdir(repositoryRoot, { withFileTypes: true });
  await Promise.all(
    entries
      .filter((entry) => entry.isFile() && copiedRootExtensions.has(path.extname(entry.name).toLowerCase()))
      .map((entry) => cp(path.join(repositoryRoot, entry.name), path.join(outputDirectory, entry.name))),
  );

  await Promise.all(
    copiedDirectories.map((directory) =>
      cp(path.join(repositoryRoot, directory), path.join(outputDirectory, directory), { recursive: true }),
    ),
  );
}

export async function buildWebAssets(
  outputDirectory = path.join(repositoryRoot, 'src', 'MeshCommander.Server', 'wwwroot'),
) {
  const resolvedOutput = path.resolve(outputDirectory);
  const expectedRoot = path.resolve(repositoryRoot, 'src', 'MeshCommander.Server');
  const temporaryRoot = path.resolve(process.env.TEMP ?? process.env.TMP ?? repositoryRoot);
  if (!resolvedOutput.startsWith(expectedRoot + path.sep) && !resolvedOutput.startsWith(temporaryRoot + path.sep)) {
    throw new Error(`Refusing to replace unexpected output directory: ${resolvedOutput}`);
  }

  await rm(resolvedOutput, { recursive: true, force: true });
  await mkdir(resolvedOutput, { recursive: true });
  await copyRootAssets(resolvedOutput);

  const htmlFiles = (await readdir(repositoryRoot)).filter((name) => /^index(?:_[a-z-]+)?\.html$/i.test(name));
  await Promise.all(
    htmlFiles.map(async (name) => {
      const source = await readFile(path.join(repositoryRoot, name), 'utf8');
      await writeFile(path.join(resolvedOutput, name), processTemplate(source, FEATURES, { tolerateUnclosed: true }), 'utf8');
    }),
  );

  await cp(
    path.join(repositoryRoot, 'modern', 'web', 'styles-enhanced.css'),
    path.join(resolvedOutput, 'styles-enhanced.css'),
  );
  await cp(
    path.join(repositoryRoot, 'modern', 'web', 'desktop-bootstrap.js'),
    path.join(resolvedOutput, 'desktop-bootstrap.js'),
  );

  await writeFile(
    path.join(resolvedOutput, 'build-info.json'),
    JSON.stringify(
      {
        name: 'MeshCommander Enhanced',
        compatibilityBase: 'MeshCommander 0.9.6',
        features: [...FEATURES].sort((left, right) => left.localeCompare(right)),
      },
      null,
      2,
    ) + '\n',
    'utf8',
  );

  return { htmlFiles, outputDirectory: resolvedOutput };
}

if (process.argv[1] && import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href) {
  const result = await buildWebAssets(process.argv[2]);
  console.log(`Built ${result.htmlFiles.length} localized web entry points in ${result.outputDirectory}`);
}
