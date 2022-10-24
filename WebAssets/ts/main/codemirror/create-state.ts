import { EditorState, EditorSelection } from '@codemirror/state';
import { indentUnit, classHighlightStyle } from '@codemirror/language';
import { history } from '@codemirror/commands';
import type { Connection } from '../connection';
import type { SlowUpdateOptions } from '../../interfaces/slow-update';
import { csharp } from './lang-csharp';
import lineSeparator from './line-separator';
import keymap from './keymap';
import { sendChangesToServer } from './server/send-changes';
import { slowUpdateLinter } from './server/slow-update-linter';
import { connectionState } from './server/connection-state';
import { infotipsFromServer } from './server/infotips';
import { autocompletionFromServer } from './server/autocompletion';

export function createState<O, U>(
    connection: Connection<O, U>,
    options: {
        initialText?: string;
        initialCursorOffset?: number;
    } & SlowUpdateOptions<U> = {}
) {
    return EditorState.create({
        ...(options.initialText ? { doc: options.initialText } : {}),
        ...(options.initialCursorOffset ? { selection: EditorSelection.single(options.initialCursorOffset) } : {}),
        extensions: [
            indentUnit.of('    '),
            EditorState.lineSeparator.of(lineSeparator),

            history(),
            csharp(),
            classHighlightStyle,

            connectionState(connection),
            sendChangesToServer(connection),
            slowUpdateLinter(connection, options),
            infotipsFromServer(connection),
            autocompletionFromServer(connection),

            // has to go last so that more specific keymaps
            // in e.g. autocomplete have more priority
            keymap
        ]
    });
}