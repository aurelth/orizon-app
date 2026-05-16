import type { Config } from 'jest';

const config: Config = {
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  testMatch: ['**/src/**/*.spec.ts'],
  transform: {
    '^.+\\.(ts|js|mjs|cjs)$': 'babel-jest',
  },
  transformIgnorePatterns: [
    'node_modules/(?!(@angular|rxjs|tslib|@ngrx|ngx-toastr)/)',
  ],
  moduleNameMapper: {
    '^@app/(.*)$': '<rootDir>/src/app/$1',
    '^@env/(.*)$': '<rootDir>/src/environments/$1',
    '\\.html$': '<rootDir>/src/__mocks__/empty.mock.ts',
    '\\.scss$': '<rootDir>/src/__mocks__/empty.mock.ts',
  },
  collectCoverageFrom: [
    'src/app/**/*.ts',
    '!src/app/**/*.routes.ts',
    '!src/main.ts',
  ],
  coverageThreshold: {
    global: {
      statements: 70,
      branches: 70,
      functions: 70,
      lines: 70,
    },
  },
};

export default config;