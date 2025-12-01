import type { KeyValueEntry, WorkflowVariableDataType } from "./types";

const generateId = () =>
  `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;

export const createKeyValueEntry = (
  key = "",
  value = "",
  dataType?: WorkflowVariableDataType
): KeyValueEntry => ({
  id: generateId(),
  key,
  value,
  dataType,
});

export const entriesFromRecord = (
  record: Record<string, string> | undefined | null,
  variableTypes?: Record<string, WorkflowVariableDataType> | undefined
) => {
  if (!record) {
    return [] as KeyValueEntry[];
  }

  return Object.entries(record).map(([key, value]) =>
    createKeyValueEntry(key, value, variableTypes?.[key])
  );
};

export const recordFromEntries = (entries: KeyValueEntry[]) => {
  return entries
    .filter((entry) => entry.key.trim().length > 0)
    .reduce<Record<string, string>>((accumulator, entry) => {
      accumulator[entry.key.trim()] = entry.value;
      return accumulator;
    }, {});
};

export const variableTypesFromEntries = (
  entries: KeyValueEntry[]
): Record<string, WorkflowVariableDataType> => {
  return entries.reduce<Record<string, WorkflowVariableDataType>>(
    (accumulator, entry) => {
      const key = entry.key.trim();
      if (key && entry.dataType) {
        accumulator[key] = entry.dataType;
      }
      return accumulator;
    },
    {}
  );
};
