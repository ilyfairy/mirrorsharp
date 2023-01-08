import { EditorState, EditorSelection, Extension } from '@codemirror/state';
import { indentUnit, syntaxHighlighting } from '@codemirror/language';
import { history } from '@codemirror/commands';
import type { Connection } from '../connection';
import type { SlowUpdateOptions } from '../../interfaces/slow-update';
import lineSeparator from './line-separator';
import keymap from './keymap';
import { sendChangesToServer } from './server/send-changes';
import { lintingFromServer } from './server/linting';
import { connectionState } from './server/connection-state';
import { infotipsFromServer } from './server/infotips';
import { autocompletionFromServer } from './server/autocompletion';
import { classHighlighter } from '@lezer/highlight';
import type { Session } from '../session';
import { languageExtensions } from './languages';
import type { Language } from '../../interfaces/protocol';
import { signatureHelpFromServer } from './server/signature-help';

export const createExtensions = <O, U>(
    connection: Connection<O, U>,
    session: Session<O>,
    options: { initialLanguage: Language } & SlowUpdateOptions<U>
): ReadonlyArray<Extension> => [
    indentUnit.of('    '),
    EditorState.lineSeparator.of(lineSeparator),

    history(),

    languageExtensions[options.initialLanguage],
    syntaxHighlighting(classHighlighter),

    connectionState(connection),
    sendChangesToServer(session as Session),
    lintingFromServer(connection as Connection<unknown, U>, options),
    infotipsFromServer(connection),
    signatureHelpFromServer(connection as Connection),
    autocompletionFromServer(connection),

    // has to go last so that more specific keymaps
    // in e.g. autocomplete have more priority
    keymap
];

export const createState = (
    extensions: ReadonlyArray<Extension>,
    options: {
        initialText?: string;
        initialCursorOffset?: number;
    } = {}
) => {
    return EditorState.create({
        ...(options.initialText ? { doc: options.initialText } : {}),
        ...(options.initialCursorOffset ? { selection: EditorSelection.single(options.initialCursorOffset) } : {}),
        extensions
    });
};